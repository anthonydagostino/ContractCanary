namespace OppSignal.Infrastructure.Email;

/// <summary>Model for the transactional single-call-to-action emails (verify, reset, welcome).</summary>
public sealed class EmailActionModel
{
    public string ProductName { get; set; } = "OppSignal";
    public string Tagline { get; set; } = "";
    public string BrandColor { get; set; } = "#1d4ed8";
    public string SupportEmail { get; set; } = "";
    public string WebBaseUrl { get; set; } = "";

    public string? Name { get; set; }
    public string Heading { get; set; } = "";
    public string Intro { get; set; } = "";
    public string? ButtonText { get; set; }
    public string? ActionUrl { get; set; }
    public string? Outro { get; set; }
}
