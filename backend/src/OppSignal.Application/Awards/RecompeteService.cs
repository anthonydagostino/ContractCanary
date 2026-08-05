using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using OppSignal.Application.Abstractions;
using OppSignal.Application.Matching;

namespace OppSignal.Application.Awards;

public sealed class RecompeteDto
{
    public string AwardId { get; set; } = "";
    public string? DisplayAwardId { get; set; }
    public string? RecipientName { get; set; }
    public string? AwardingAgency { get; set; }
    public string? NaicsCode { get; set; }
    public string? PscCode { get; set; }
    public decimal? ObligatedAmount { get; set; }
    public decimal? PotentialTotalValue { get; set; }
    public string? PopState { get; set; }
    public DateTime? PeriodOfPerformanceEnd { get; set; }
    /// <summary>When the agency has likely started (or will start) rebidding.</summary>
    public DateTime? RecompeteWindowOpens { get; set; }
    public bool RecompeteWindowOpen { get; set; }
    public List<string> MatchedProfileNames { get; set; } = new();
    /// <summary>Deep link to the public award record.</summary>
    public string UsaSpendingUrl { get; set; } = "";
}

public sealed class RecompetePage
{
    public List<RecompeteDto> Items { get; set; } = new();
    public int Total { get; set; }
    public int Page { get; set; }
    public int TotalPages { get; set; }
}

public interface IRecompeteService
{
    Task<RecompetePage> ListAsync(Guid userId, int page, int pageSize, CancellationToken ct = default);
}

/// <summary>Pure date math for the recompete window, unit-testable.</summary>
public static class RecompeteMath
{
    /// <summary>
    /// Agencies typically begin market research / rebidding ~12 months before
    /// the incumbent's period of performance ends.
    /// </summary>
    public const int WindowMonthsBeforeEnd = 12;

    public static DateTime? WindowOpens(DateTime? periodOfPerformanceEnd)
        => periodOfPerformanceEnd?.AddMonths(-WindowMonthsBeforeEnd);

    public static bool IsWindowOpen(DateTime? periodOfPerformanceEnd, DateTime nowUtc)
        => WindowOpens(periodOfPerformanceEnd) is { } opens && opens <= nowUtc;
}

/// <summary>
/// A user's expiring incumbent contracts: awards whose NAICS falls under any
/// code watched by their active profiles and whose period of performance ends
/// within the configured window — i.e. predicted recompete opportunities.
/// </summary>
public sealed class RecompeteService : IRecompeteService
{
    // Applied PER watched code (a constant StartsWith translates to LIKE), so
    // one user's busy code can never crowd another user's awards out of a
    // shared global scan.
    private const int ScanCapPerCode = 2000;

    private readonly IAppDbContext _db;
    private readonly IClock _clock;
    private readonly AwardsOptions _options;

    public RecompeteService(IAppDbContext db, IClock clock, IOptions<AwardsOptions> options)
    {
        _db = db;
        _clock = clock;
        _options = options.Value;
    }

    public async Task<RecompetePage> ListAsync(Guid userId, int page, int pageSize, CancellationToken ct = default)
    {
        // Upper clamp keeps (page-1)*pageSize far from int overflow, where a
        // wrapped-negative Skip would relabel page 1 as the requested page.
        page = Math.Clamp(page, 1, 100_000);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var profiles = await _db.MatchProfiles.AsNoTracking()
            .Where(p => p.UserId == userId && p.IsActive)
            .Select(p => new { p.Name, p.Naics })
            .ToListAsync(ct);

        var watched = profiles
            .SelectMany(p => p.Naics.Select(code => (Code: code.Trim(), p.Name)))
            .Where(x => x.Code.Length > 0)
            .ToList();
        if (watched.Count == 0) return new RecompetePage { Page = page, TotalPages = 0 };

        var now = _clock.UtcNow;
        // Calendar-day boundary: an award expiring later today is still a live
        // recompete signal (PoP ends are stored as midnight UTC).
        var windowFrom = now.Date;
        var endTo = now.AddMonths(Math.Max(1, _options.WindowMonths));

        var byId = new Dictionary<string, Domain.Entities.Award>(StringComparer.Ordinal);
        foreach (var code in watched.Select(w => w.Code).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            var rows = await _db.Awards.AsNoTracking()
                .Where(a => a.PeriodOfPerformanceEnd != null
                    && a.PeriodOfPerformanceEnd >= windowFrom
                    && a.PeriodOfPerformanceEnd <= endTo
                    && a.NaicsCode != null
                    && a.NaicsCode.StartsWith(code))
                .OrderBy(a => a.PeriodOfPerformanceEnd)
                .ThenBy(a => a.AwardId)
                .Take(ScanCapPerCode)
                .ToListAsync(ct);
            foreach (var a in rows) byId[a.AwardId] = a;
        }

        var candidates = byId.Values
            .OrderBy(a => a.PeriodOfPerformanceEnd)
            .ThenBy(a => a.AwardId, StringComparer.Ordinal)
            .ToList();

        var matched = candidates
            .Select(a => new
            {
                Award = a,
                Profiles = watched
                    .Where(w => MatchEngine.CodeCovers(w.Code, a.NaicsCode!))
                    .Select(w => w.Name)
                    .Distinct()
                    .ToList(),
            })
            .Where(x => x.Profiles.Count > 0)
            .ToList();

        var items = matched
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new RecompeteDto
            {
                AwardId = x.Award.AwardId,
                DisplayAwardId = x.Award.DisplayAwardId,
                RecipientName = x.Award.RecipientName,
                AwardingAgency = x.Award.AwardingAgency,
                NaicsCode = x.Award.NaicsCode,
                PscCode = x.Award.PscCode,
                ObligatedAmount = x.Award.ObligatedAmount,
                PotentialTotalValue = x.Award.PotentialTotalValue,
                PopState = x.Award.PopState,
                PeriodOfPerformanceEnd = x.Award.PeriodOfPerformanceEnd,
                RecompeteWindowOpens = RecompeteMath.WindowOpens(x.Award.PeriodOfPerformanceEnd),
                RecompeteWindowOpen = RecompeteMath.IsWindowOpen(x.Award.PeriodOfPerformanceEnd, now),
                MatchedProfileNames = x.Profiles,
                UsaSpendingUrl = $"https://www.usaspending.gov/award/{Uri.EscapeDataString(x.Award.AwardId)}",
            })
            .ToList();

        return new RecompetePage
        {
            Items = items,
            Total = matched.Count,
            Page = page,
            TotalPages = (int)Math.Ceiling(matched.Count / (double)pageSize),
        };
    }
}
