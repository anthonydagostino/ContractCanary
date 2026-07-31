namespace OppSignal.Application.Ingest;

/// <summary>Ingest behavior + scheduling knobs (bound from the <c>Ingest</c> config section).</summary>
public class IngestOptions
{
    public const string SectionName = "Ingest";

    /// <summary>"Fixture" (default, no credentials) or "Sam" (real API).</summary>
    public string Source { get; set; } = "Fixture";

    /// <summary>Worker schedule: minutes between ingest runs. 180 → 8×/day.</summary>
    public int IntervalMinutes { get; set; } = 180;

    /// <summary>Rolling posted-date window pulled each run (days). 3 re-catches late updates.</summary>
    public int WindowDays { get; set; } = 3;

    /// <summary>Page size (SAM caps at 1000).</summary>
    public int PageSize { get; set; } = 1000;

    /// <summary>Defensive per-run cap on API requests so we never blow the daily rate limit.</summary>
    public int MaxPagesPerRun { get; set; } = 25;

    public bool IsFixture => string.Equals(Source, "Fixture", StringComparison.OrdinalIgnoreCase);
}
