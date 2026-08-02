namespace OppSignal.Application.Common;

/// <summary>
/// CSV cell encoding: RFC4180 quoting plus spreadsheet formula-injection defense.
/// A cell whose value begins with a formula-trigger character is prefixed with a
/// single quote so Excel/Sheets treat it as text, never executing it. Pure and
/// unit-tested — exported opportunity fields carry attacker-influenceable text.
/// </summary>
public static class CsvUtil
{
    public static string Cell(string? value)
    {
        if (string.IsNullOrEmpty(value)) return "";

        var v = value;
        // Neutralize formula injection: leading = + - @ (or a control char a spreadsheet
        // may treat as a formula lead) → prefix with an apostrophe so it's parsed as text.
        var first = v[0];
        if (first is '=' or '+' or '-' or '@' or '\t' or '\r' or '\n')
            v = "'" + v;

        var needsQuote = v.Contains(',') || v.Contains('"') || v.Contains('\n') || v.Contains('\r');
        var escaped = v.Replace("\"", "\"\"");
        return needsQuote ? $"\"{escaped}\"" : escaped;
    }
}
