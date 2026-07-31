namespace OppSignal.Domain.Entities;

/// <summary>
/// v2 SEAM — intentionally empty in v1. This table exists so the future
/// award-history / recompete-expiry intelligence (sourced from USAspending.gov)
/// has a home with no schema migration surprise. Nothing ingests into it in v1;
/// see <c>IAwardIngestionStub</c> for the documented no-op ingestion stub.
/// </summary>
public class Award
{
    /// <summary>USAspending award id (natural key).</summary>
    public string AwardId { get; set; } = default!;

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
