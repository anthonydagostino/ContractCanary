namespace OppSignal.Application.Ai;

/// <summary>
/// AI opportunity-summary config (bound from the <c>Ai</c> section / <c>Ai__*</c> env).
/// Disabled by default: with no API key the feature is a no-op and the app runs
/// exactly as before. Set Enabled=true + ApiKey to switch it on.
/// </summary>
public class AiOptions
{
    public const string SectionName = "Ai";

    /// <summary>Master switch. When false (default), no summaries are generated.</summary>
    public bool Enabled { get; set; }

    /// <summary>Anthropic API key (supplied via env; never committed).</summary>
    public string? ApiKey { get; set; }

    /// <summary>
    /// Model id. Default is the cost-effective bulk model; set to a larger model
    /// (e.g. "claude-opus-5") for maximum quality. One summary is generated per
    /// notice and reused for every user, so cost scales with new notices, not users.
    /// </summary>
    public string Model { get; set; } = "claude-haiku-4-5";

    public string BaseUrl { get; set; } = "https://api.anthropic.com";
    public string AnthropicVersion { get; set; } = "2023-06-01";

    /// <summary>Worker schedule: minutes between enrichment passes.</summary>
    public int IntervalMinutes { get; set; } = 10;

    /// <summary>Cap on notices summarized per pass (bounds burst cost/latency).</summary>
    public int MaxNoticesPerRun { get; set; } = 40;

    /// <summary>Give up on a notice after this many failed attempts.</summary>
    public int MaxAttempts { get; set; } = 3;

    /// <summary>Truncate the source description to bound input tokens/cost.</summary>
    public int MaxDescriptionChars { get; set; } = 8000;

    /// <summary>Output token ceiling for a single short structured summary.</summary>
    public int MaxOutputTokens { get; set; } = 700;

    public int TimeoutSeconds { get; set; } = 60;
}
