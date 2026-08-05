namespace OppSignal.Application.Awards;

/// <summary>
/// Recompete Radar configuration. Award data comes from USAspending.gov's free
/// public API (no key). <c>Source=Fixture</c> serves deterministic sample data
/// for dev/tests; <c>Enabled=false</c> turns the whole feature's ingest off.
/// </summary>
public class AwardsOptions
{
    public const string SectionName = "Awards";

    public bool Enabled { get; set; } = true;

    /// <summary>"UsaSpending" (live) or "Fixture" (deterministic sample data).</summary>
    public string Source { get; set; } = "UsaSpending";

    public string BaseUrl { get; set; } = "https://api.usaspending.gov";

    /// <summary>How far ahead to look for expiring incumbent contracts.</summary>
    public int WindowMonths { get; set; } = 18;

    /// <summary>Rows per API page (USAspending max is 100).</summary>
    public int PageSize { get; set; } = 100;

    /// <summary>Safety cap on pages fetched per NAICS code per run.</summary>
    public int MaxPagesPerCode { get; set; } = 10;

    /// <summary>Ingest cadence. Award data changes slowly; daily is plenty.</summary>
    public int IntervalMinutes { get; set; } = 1440;
}
