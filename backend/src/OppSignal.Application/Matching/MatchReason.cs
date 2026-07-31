namespace OppSignal.Application.Matching;

/// <summary>
/// Structured explanation of why a notice matched a profile. Persisted as JSON on
/// <c>NoticeMatch.MatchReason</c> and surfaced on the opportunity detail page.
/// A null field means that filter was not set on the profile (no constraint).
/// </summary>
public sealed class MatchReason
{
    /// <summary>Which code filter satisfied the (NAICS OR PSC) clause: "naics", "psc", "both", or "none".</summary>
    public string CodeClause { get; set; } = "none";

    public string? MatchedNaics { get; set; }
    public string? MatchedPsc { get; set; }

    /// <summary>Keywords (from the profile) that were found in the title/description.</summary>
    public List<string>? MatchedKeywords { get; set; }

    public string? MatchedAgencyPath { get; set; }
    public string? MatchedSetAside { get; set; }
    public string? MatchedState { get; set; }
    public string? MatchedNoticeType { get; set; }
}

/// <summary>Result of evaluating a single (notice, profile) pair.</summary>
public readonly record struct MatchOutcome(bool IsMatch, MatchReason Reason);
