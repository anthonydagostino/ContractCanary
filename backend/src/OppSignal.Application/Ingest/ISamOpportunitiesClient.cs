using OppSignal.Domain.Enums;

namespace OppSignal.Application.Ingest;

/// <summary>
/// Port over the SAM.gov Get Opportunities Public API v2. Implemented by the real
/// HTTP client and by the fixture generator, selected by the <c>Ingest:Source</c>
/// config value. Both honor the same date-window + paging contract.
/// </summary>
public interface ISamOpportunitiesClient
{
    /// <summary>Which upstream this client represents (audited on the ingest run).</summary>
    IngestSource Source { get; }

    /// <summary>
    /// Fetch one page of notices posted within [postedFrom, postedTo] (inclusive).
    /// Dates are calendar dates (the API takes MM/dd/yyyy). <paramref name="limit"/>
    /// is capped at 1000 by the API.
    /// </summary>
    Task<SamPage> FetchPageAsync(
        DateOnly postedFrom, DateOnly postedTo, int limit, int offset, CancellationToken ct = default);
}
