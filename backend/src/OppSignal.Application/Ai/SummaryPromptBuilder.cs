using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using OppSignal.Application.Common;
using OppSignal.Domain.Entities;

namespace OppSignal.Application.Ai;

/// <summary>
/// Builds the system + user prompt for summarizing a single opportunity. Pure and
/// unit-tested. Deliberately instructs the model to work only from the supplied
/// facts and never to invent specifics (deadlines/dollar figures) — the UI shows
/// the authoritative deadline from the structured data, not from this summary.
/// </summary>
public static class SummaryPromptBuilder
{
    public const string System =
        "You help small U.S. government contractors quickly triage federal contract " +
        "opportunities from SAM.gov. Given the details of one opportunity, write a short, " +
        "plain-English overview a busy small-business owner can skim in seconds.\n\n" +
        "Rules:\n" +
        "- Use ONLY the information provided. Do not invent requirements, dollar amounts, " +
        "dates, or eligibility that are not stated.\n" +
        "- If details are thin, say so briefly rather than guessing.\n" +
        "- Be concrete and neutral. No marketing language, no hype, no emoji.\n" +
        "- Do not restate the exact response deadline as a fact — it is shown separately.\n" +
        "- 'fitNote' should give a one-sentence bid/no-bid consideration (who this suits, " +
        "or a caution), not a hard recommendation.";

    public static string BuildUser(Notice notice, int maxDescriptionChars = 8000)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Summarize this federal contract opportunity.").AppendLine();
        Line(sb, "Title", notice.Title);
        Line(sb, "Agency", notice.AgencyPath ?? notice.DepartmentName);
        Line(sb, "Notice type", SamMappings.NoticeTypeLabel(notice.Type));
        Line(sb, "NAICS code", notice.NaicsCode);
        Line(sb, "PSC / classification", notice.PscCode);
        Line(sb, "Set-aside", notice.SetAsideDescription ?? SamMappings.SetAsideName(notice.SetAside));
        Line(sb, "Place of performance", JoinPlace(notice));
        Line(sb, "Posted", notice.PostedDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));

        var description = Clean(notice.Description);
        if (!string.IsNullOrEmpty(description))
        {
            if (description.Length > maxDescriptionChars)
                description = description[..maxDescriptionChars] + " …[truncated]";
            sb.AppendLine().AppendLine("Description:").AppendLine(description);
        }
        else
        {
            sb.AppendLine().AppendLine("Description: (not provided — summarize from the fields above and note that details are limited.)");
        }

        return sb.ToString();
    }

    private static void Line(StringBuilder sb, string label, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value)) sb.Append(label).Append(": ").AppendLine(value.Trim());
    }

    private static string? JoinPlace(Notice n)
    {
        var parts = new[] { n.PopCity, n.PopState }.Where(s => !string.IsNullOrWhiteSpace(s));
        return string.Join(", ", parts);
    }

    private static string? Clean(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return null;
        // Description may arrive as HTML; strip tags to keep the prompt lean.
        var noTags = Regex.Replace(s, "<[^>]+>", " ");
        var collapsed = Regex.Replace(noTags, "\\s+", " ").Trim();
        return collapsed.Length == 0 ? null : collapsed;
    }
}
