namespace OppSignal.Application.Awards;

/// <summary>
/// One prime-award record from the award data source, normalized to what the
/// Recompete Radar needs. <c>AwardKey</c> must be globally unique and stable
/// (USAspending's generated_internal_id); <c>DisplayId</c> is the human PIID.
/// </summary>
public sealed record AwardRecord(
    string AwardKey,
    string? DisplayId,
    string? RecipientName,
    string? RecipientUei,
    string? AwardingAgency,
    string? NaicsCode,
    string? PscCode,
    decimal? ObligatedAmount,
    decimal? PotentialTotalValue,
    DateTime? PeriodOfPerformanceStart,
    DateTime? PeriodOfPerformanceEnd,
    string? PopState,
    string RawJson);

/// <summary>
/// Port to the award-history data source (USAspending.gov live, or fixture).
/// Returns prime contract awards under <paramref name="naicsCode"/> whose
/// period of performance ends inside [endFrom, endTo].
/// </summary>
public interface IAwardsClient
{
    string Source { get; }

    Task<IReadOnlyList<AwardRecord>> FetchExpiringAwardsAsync(
        string naicsCode, DateOnly endFrom, DateOnly endTo, CancellationToken ct = default);
}
