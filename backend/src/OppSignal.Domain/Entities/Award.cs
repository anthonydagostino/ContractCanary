namespace OppSignal.Domain.Entities;

/// <summary>
/// A prime contract award from USAspending.gov, ingested for the Recompete
/// Radar: awards whose period of performance ends soon are predicted rebids.
/// Scope is limited to NAICS codes watched by active match profiles.
/// </summary>
public class Award
{
    /// <summary>USAspending generated_internal_id (natural key; used in deep links).</summary>
    public string AwardId { get; set; } = default!;

    /// <summary>Human-readable award number (PIID) for display.</summary>
    public string? DisplayAwardId { get; set; }

    public string? RecipientName { get; set; }
    public string? RecipientUei { get; set; }
    public string? AwardingAgency { get; set; }
    public string? NaicsCode { get; set; }
    public string? PscCode { get; set; }

    public decimal? ObligatedAmount { get; set; }
    public decimal? PotentialTotalValue { get; set; }

    public DateTime? PeriodOfPerformanceStart { get; set; }
    /// <summary>The signal for v2: when this ends, a recompete is likely coming.</summary>
    public DateTime? PeriodOfPerformanceEnd { get; set; }

    public string? PopState { get; set; }

    public string RawJson { get; set; } = "{}";
    public DateTime IngestedAt { get; set; }
}
