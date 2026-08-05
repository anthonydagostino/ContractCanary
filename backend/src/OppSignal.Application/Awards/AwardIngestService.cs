using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OppSignal.Application.Abstractions;
using OppSignal.Domain.Entities;

namespace OppSignal.Application.Awards;

public interface IAwardIngestService
{
    /// <summary>
    /// Pull expiring prime awards for every NAICS code used by an active match
    /// profile and upsert them into the Award table. Idempotent. Returns the
    /// number of awards inserted or updated.
    /// </summary>
    Task<int> RunAsync(CancellationToken ct = default);
}

/// <summary>
/// Recompete Radar ingest. Most federal contracts run ≤5 years, and the agency
/// typically rebids 12–18 months before the period of performance ends — so an
/// award expiring inside the window is a predicted opportunity before anything
/// is posted on SAM.gov. Scope is bounded to codes users actually watch.
/// </summary>
public sealed class AwardIngestService : IAwardIngestService
{
    private readonly IAppDbContext _db;
    private readonly IAwardsClient _client;
    private readonly IClock _clock;
    private readonly AwardsOptions _options;
    private readonly ILogger<AwardIngestService> _log;

    public AwardIngestService(
        IAppDbContext db,
        IAwardsClient client,
        IClock clock,
        IOptions<AwardsOptions> options,
        ILogger<AwardIngestService> log)
    {
        _db = db;
        _client = client;
        _clock = clock;
        _options = options.Value;
        _log = log;
    }

    public async Task<int> RunAsync(CancellationToken ct = default)
    {
        if (!_options.Enabled) return 0;

        // Every NAICS code any active profile watches (codes may be 2–6 digit
        // prefixes; the client/source resolves them hierarchically).
        var codes = (await _db.MatchProfiles.AsNoTracking()
                .Where(p => p.IsActive)
                .Select(p => p.Naics)
                .ToListAsync(ct))
            .SelectMany(list => list)
            .Where(c => !string.IsNullOrWhiteSpace(c))
            .Select(c => c.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (codes.Count == 0) return 0;

        var today = DateOnly.FromDateTime(_clock.UtcNow);
        var endTo = today.AddMonths(Math.Max(1, _options.WindowMonths));

        var touched = 0;
        foreach (var code in codes)
        {
            IReadOnlyList<AwardRecord> records;
            try
            {
                records = await _client.FetchExpiringAwardsAsync(code, today, endTo, ct);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                // One code's failure (rate limit, transient outage) must not
                // sink the rest — awards refresh again tomorrow.
                _log.LogWarning(ex, "Award fetch failed for NAICS {Code}; continuing", code);
                continue;
            }

            touched += await UpsertAsync(records, ct);
        }

        if (touched > 0)
            _log.LogInformation("Award ingest ({Source}): {Count} awards inserted/updated across {Codes} NAICS codes",
                _client.Source, touched, codes.Count);
        return touched;
    }

    private async Task<int> UpsertAsync(IReadOnlyList<AwardRecord> records, CancellationToken ct)
    {
        if (records.Count == 0) return 0;

        var keys = records.Select(r => r.AwardKey).Distinct().ToList();
        var existing = await _db.Awards
            .Where(a => keys.Contains(a.AwardId))
            .ToDictionaryAsync(a => a.AwardId, ct);

        var now = _clock.UtcNow;
        var touched = 0;
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var r in records)
        {
            if (string.IsNullOrWhiteSpace(r.AwardKey) || !seen.Add(r.AwardKey)) continue;

            if (!existing.TryGetValue(r.AwardKey, out var row))
            {
                row = new Award { AwardId = r.AwardKey };
                _db.Awards.Add(row);
            }

            row.DisplayAwardId = r.DisplayId;
            row.RecipientName = r.RecipientName;
            row.RecipientUei = r.RecipientUei;
            row.AwardingAgency = r.AwardingAgency;
            row.NaicsCode = r.NaicsCode;
            row.PscCode = r.PscCode;
            row.ObligatedAmount = r.ObligatedAmount;
            row.PotentialTotalValue = r.PotentialTotalValue;
            row.PeriodOfPerformanceStart = Utc(r.PeriodOfPerformanceStart);
            row.PeriodOfPerformanceEnd = Utc(r.PeriodOfPerformanceEnd);
            row.PopState = r.PopState;
            row.RawJson = string.IsNullOrWhiteSpace(r.RawJson) ? "{}" : r.RawJson;
            row.IngestedAt = now;
            touched++;
        }

        await _db.SaveChangesAsync(ct);
        return touched;
    }

    private static DateTime? Utc(DateTime? d)
        => d is { } v ? DateTime.SpecifyKind(v, DateTimeKind.Utc) : null;
}
