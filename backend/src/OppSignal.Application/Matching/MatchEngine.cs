using OppSignal.Domain.Entities;

namespace OppSignal.Application.Matching;

/// <summary>
/// Deterministic, dependency-free matching engine. A notice matches a profile iff
/// the (NAICS OR PSC) code clause holds AND every set filter passes. An unset
/// (null/empty) filter is treated as "no constraint" and passes.
///
/// This type has NO I/O — it is the exhaustively unit-tested core.
/// </summary>
public sealed class MatchEngine : IMatchEngine
{
    public MatchOutcome Evaluate(Notice notice, MatchProfile profile)
    {
        var reason = new MatchReason();

        // ---- (NAICS OR PSC) code clause -------------------------------------
        var naicsSet = profile.Naics is { Count: > 0 };
        var pscSet = profile.Psc is { Count: > 0 };

        var naicsHit = naicsSet
            && !string.IsNullOrEmpty(notice.NaicsCode)
            && profile.Naics.Any(c => CodeCovers(c, notice.NaicsCode!));
        var pscHit = pscSet
            && !string.IsNullOrEmpty(notice.PscCode)
            && profile.Psc.Any(c => CodeCovers(c, notice.PscCode!));

        bool codeClause;
        if (!naicsSet && !pscSet)
        {
            codeClause = true;
            reason.CodeClause = "none";
        }
        else if (naicsSet && pscSet)
        {
            codeClause = naicsHit || pscHit;
            reason.CodeClause = naicsHit && pscHit ? "both" : naicsHit ? "naics" : pscHit ? "psc" : "none";
        }
        else if (naicsSet)
        {
            codeClause = naicsHit;
            reason.CodeClause = naicsHit ? "naics" : "none";
        }
        else
        {
            codeClause = pscHit;
            reason.CodeClause = pscHit ? "psc" : "none";
        }

        if (naicsHit) reason.MatchedNaics = notice.NaicsCode;
        if (pscHit) reason.MatchedPsc = notice.PscCode;

        if (!codeClause) return new MatchOutcome(false, reason);

        // ---- Keyword filter (substring over title + description) ------------
        if (profile.Keywords is { Count: > 0 })
        {
            var haystack = ((notice.Title ?? string.Empty) + " " + (notice.Description ?? string.Empty));
            var hits = profile.Keywords
                .Where(k => !string.IsNullOrWhiteSpace(k))
                .Where(k => haystack.Contains(k.Trim(), StringComparison.OrdinalIgnoreCase))
                .ToList();
            if (hits.Count == 0) return new MatchOutcome(false, reason);
            reason.MatchedKeywords = hits;
        }

        // ---- Agency filter (prefix or path-segment match) -------------------
        if (profile.AgencyPaths is { Count: > 0 })
        {
            var matched = profile.AgencyPaths
                .Where(a => !string.IsNullOrWhiteSpace(a))
                .FirstOrDefault(a => AgencyPathMatches(notice.AgencyPath, a));
            if (matched is null) return new MatchOutcome(false, reason);
            reason.MatchedAgencyPath = matched;
        }

        // ---- Set-aside filter -----------------------------------------------
        if (profile.SetAsides is { Count: > 0 })
        {
            if (!profile.SetAsides.Contains(notice.SetAside)) return new MatchOutcome(false, reason);
            reason.MatchedSetAside = notice.SetAside.ToString();
        }

        // ---- Place-of-performance state filter ------------------------------
        if (profile.States is { Count: > 0 })
        {
            if (string.IsNullOrEmpty(notice.PopState)
                || !profile.States.Contains(notice.PopState!, StringComparer.OrdinalIgnoreCase))
                return new MatchOutcome(false, reason);
            reason.MatchedState = notice.PopState;
        }

        // ---- Notice-type filter ---------------------------------------------
        if (profile.NoticeTypes is { Count: > 0 })
        {
            if (!profile.NoticeTypes.Contains(notice.Type)) return new MatchOutcome(false, reason);
            reason.MatchedNoticeType = notice.Type.ToString();
        }

        return new MatchOutcome(true, reason);
    }

    /// <summary>
    /// NAICS and PSC are hierarchical by character prefix: sector "54" covers
    /// "541511", PSC category "D" covers "D302". The reference typeahead serves
    /// codes at every level, so a selected code must cover its whole subtree —
    /// exact-equality matching would make sector-level profiles match nothing.
    /// </summary>
    internal static bool CodeCovers(string selected, string noticeCode)
    {
        var sel = selected.Trim();
        return sel.Length > 0 && noticeCode.StartsWith(sel, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// True if <paramref name="selected"/> is a prefix of the notice's agency path
    /// (departments sit at the root) OR equals one of its "."-delimited segments
    /// (sub-tiers), case-insensitive.
    /// </summary>
    internal static bool AgencyPathMatches(string? agencyPath, string selected)
    {
        if (string.IsNullOrEmpty(agencyPath)) return false;
        var sel = selected.Trim();
        if (agencyPath.StartsWith(sel, StringComparison.OrdinalIgnoreCase)) return true;
        foreach (var seg in agencyPath.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            if (seg.Equals(sel, StringComparison.OrdinalIgnoreCase)) return true;
        return false;
    }
}
