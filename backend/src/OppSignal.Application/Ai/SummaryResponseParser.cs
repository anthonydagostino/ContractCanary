using System.Text.Json;

namespace OppSignal.Application.Ai;

/// <summary>
/// Parses the Anthropic Messages API response into an <see cref="OpportunityAiSummary"/>.
/// Pure and unit-tested. Returns null on a refusal or malformed output rather than
/// throwing, so the enrichment loop can skip a bad record and move on.
/// </summary>
public static class SummaryResponseParser
{
    /// <summary>Parse a full /v1/messages response body.</summary>
    public static OpportunityAiSummary? Parse(string responseJson, string model)
    {
        if (string.IsNullOrWhiteSpace(responseJson)) return null;

        try
        {
            using var doc = JsonDocument.Parse(responseJson);
            var root = doc.RootElement;

            // A safety refusal returns stop_reason "refusal" and no usable content.
            if (root.TryGetProperty("stop_reason", out var stop) &&
                stop.ValueKind == JsonValueKind.String &&
                string.Equals(stop.GetString(), "refusal", StringComparison.OrdinalIgnoreCase))
                return null;

            if (!root.TryGetProperty("content", out var content) || content.ValueKind != JsonValueKind.Array)
                return null;

            foreach (var block in content.EnumerateArray())
            {
                if (block.TryGetProperty("type", out var t) && t.GetString() == "text" &&
                    block.TryGetProperty("text", out var text) && text.ValueKind == JsonValueKind.String)
                {
                    var parsed = ParseSummaryObject(text.GetString(), model);
                    if (parsed is not null) return parsed;
                }
            }
            return null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>
    /// Parse the inner structured JSON object ({summary, keyPoints[], fitNote}).
    /// Tolerates surrounding prose by extracting the first {...} span.
    /// </summary>
    public static OpportunityAiSummary? ParseSummaryObject(string? jsonText, string model)
    {
        if (string.IsNullOrWhiteSpace(jsonText)) return null;

        var span = ExtractJsonObject(jsonText);
        if (span is null) return null;

        try
        {
            using var doc = JsonDocument.Parse(span);
            var root = doc.RootElement;
            if (root.ValueKind != JsonValueKind.Object) return null;

            var summary = GetString(root, "summary");
            if (string.IsNullOrWhiteSpace(summary)) return null;

            var fitNote = GetString(root, "fitNote") ?? "";

            var keyPoints = new List<string>();
            if (root.TryGetProperty("keyPoints", out var kp) && kp.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in kp.EnumerateArray())
                {
                    if (item.ValueKind == JsonValueKind.String)
                    {
                        var s = item.GetString();
                        if (!string.IsNullOrWhiteSpace(s)) keyPoints.Add(s.Trim());
                    }
                }
            }

            return new OpportunityAiSummary(summary.Trim(), keyPoints, fitNote.Trim(), model);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string? GetString(JsonElement obj, string name) =>
        obj.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;

    private static string? ExtractJsonObject(string text)
    {
        var start = text.IndexOf('{');
        var end = text.LastIndexOf('}');
        if (start < 0 || end <= start) return null;
        return text.Substring(start, end - start + 1);
    }
}
