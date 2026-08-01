namespace OppSignal.Application.Ai;

/// <summary>The structured result of summarizing one opportunity.</summary>
public sealed record OpportunityAiSummary(
    string Summary,
    IReadOnlyList<string> KeyPoints,
    string FitNote,
    string Model);

/// <summary>
/// Turns a normalized notice into a shared, plain-English overview. One
/// implementation calls Claude; the disabled implementation is a no-op used when
/// the feature is off. Summaries are generated once per notice and reused for all users.
/// </summary>
public interface IOpportunitySummarizer
{
    /// <summary>True when a real provider is configured (enables the enrichment job).</summary>
    bool Enabled { get; }

    Task<OpportunityAiSummary?> SummarizeAsync(
        Domain.Entities.Notice notice, CancellationToken ct = default);
}
