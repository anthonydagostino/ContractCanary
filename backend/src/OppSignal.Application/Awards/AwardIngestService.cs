using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OppSignal.Application.Abstractions;
using OppSignal.Domain.Entities;

namespace OppSignal.Application.Awards;

/// <summary>Outcome of one award-ingest run, so ops can tell "nothing to do" from "everything failed".</summary>
public sealed record AwardIngestResult(int AwardsUpserted, int CodesProcessed, int CodesFailed);

public interface IAwardIngestService
{
    /// <summary>
    /// Pull expiring prime awards for every NAICS code used by an active match
    /// profile and upsert them into the Award table. Idempotent.
    /// </summary>
    Task<AwardIngestResult> RunAsync(CancellationToken ct = default);
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

    public async Task<AwardIngestResult> RunAsync(CancellationToken ct = default)
    {
        if (!_options.Enabled) return new AwardIngestResult(0, 0, 0);

        // Every NAICS code any active profile watches (codes may be 2–6 digit
        // prefixes; the client/source resolves them hierarchically). Non-digit
        // garbage would just burn a guaranteed-failing API call per day.
        var codes = (await _db.MatchProfiles.AsNoTracking()
                .Where(p => p.IsActive)
                .Select(p => p.Naics)
                .ToListAsync(ct))
            .SelectMany(list => list)
            .Where(c => !string.IsNullOrWhiteSpace(c))
            .Select(c => c.Trim())
            .Where(c => c.Length is >= 2 and <= 6 && c.All(char.IsAsciiDigit))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (codes.Count == 0) return new AwardIngestResult(0, 0, 0);

        var today = DateOnly.FromDateTime(_clock.UtcNow);
        var endTo = today.AddMonths(Math.Max(1, _options.WindowMonths));

        // Hygiene: awards whose recompete has long passed are dead weight; the
        // radar never shows them (it filters end >= now) so drop them.
        var pruneCutoff = _clock.UtcNow.AddDays(-90);
        await _db.Awards
            .Where(a => a.PeriodOfPerformanceEnd != null && a.PeriodOfPerformanceEnd < pruneCutoff)
            .ExecuteDeleteAsync(ct);

        var touched = 0;
        var failed = 0;
        foreach (var code in codes)
        {
            try
            {
                var records = await _client.FetchExpiringAwardsAsync(code, today, endTo, ct);
                touched += await UpsertAsync(records, ct);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                // One code's failure (rate limit, outage, or a unique-key race
                // with an ingest running in the other process) must not sink
                // the rest — and a failed SaveChanges must not leave poisoned
                // entities in the tracker for the next code's save.
                _db.ClearChangeTracker();
                failed++;
                _log.LogWarning(ex, "Award ingest failed for NAICS {Code}; continuing with remaining codes", code);
            }
        }

        _log.LogInformation("Award ingest ({Source}): {Count} awards inserted/updated across {Codes} NAICS codes ({Failed} failed)",
            _client.Source, touched, codes.Count, failed);
        return new AwardIngestResult(touched, codes.Count, failed);
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
            if (r.AwardKey.Length > 128)
            {
                // Longer than the key column: skipping one record beats a
                // DbUpdateException that rolls back the whole batch — and would
                // recur every run, permanently starving this code.
                _log.LogWarning("Skipping award with oversize key ({Length} chars): {Key}", r.AwardKey.Length, r.AwardKey[..40]);
                continue;
            }

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
            // Postgres jsonb rejects NUL (\\u0000) escapes; strip them rather than let
            // one odd payload roll back the batch.
            var raw = string.IsNullOrWhiteSpace(r.RawJson) ? "{}" : r.RawJson;
            row.RawJson = raw.Contains("\\u0000", StringComparison.Ordinal) ? raw.Replace("\\u0000", "", StringComparison.Ordinal) : raw;
            row.IngestedAt = now;
            touched++;
        }

        await _db.SaveChangesAsync(ct);
        return touched;
    }

    private static DateTime? Utc(DateTime? d)
        => d is { } v ? DateTime.SpecifyKind(v, DateTimeKind.Utc) : null;
}
