using OppSignal.Application.Ai;
using OppSignal.Domain.Entities;

namespace OppSignal.Infrastructure.Ai;

/// <summary>
/// No-op summarizer used when the AI feature is off (no key configured). Keeps the
/// rest of the app running unchanged; the enrichment job sees Enabled=false and skips.
/// </summary>
public sealed class DisabledOpportunitySummarizer : IOpportunitySummarizer
{
    public bool Enabled => false;

    public Task<OpportunityAiSummary?> SummarizeAsync(Notice notice, CancellationToken ct = default)
        => Task.FromResult<OpportunityAiSummary?>(null);
}
