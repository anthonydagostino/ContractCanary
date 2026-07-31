namespace OppSignal.Infrastructure.Ingest;

/// <summary>Real SAM.gov API client config (bound from the <c>Sam</c> section / <c>Sam__*</c> env).</summary>
public class SamOptions
{
    public const string SectionName = "Sam";

    /// <summary>Base URL. Prod: https://api.sam.gov ; alpha: https://api-alpha.sam.gov/prodlike</summary>
    public string BaseUrl { get; set; } = "https://api.sam.gov";

    /// <summary>Account API key (from SAM.gov Account Details). Supplied by the human via env.</summary>
    public string? ApiKey { get; set; }

    /// <summary>Pass the key as the X-Api-Key header instead of the api_key query param.</summary>
    public bool UseHeaderAuth { get; set; }

    public int RetryCount { get; set; } = 4;
    public int TimeoutSeconds { get; set; } = 60;
}
