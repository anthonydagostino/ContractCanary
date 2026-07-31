namespace OppSignal.Application.Common;

/// <summary>
/// ONE of the two branding config spots (the other is
/// <c>frontend/src/config/branding.ts</c>). Bound from the <c>Branding</c>
/// config section; override any value with a <c>Branding__*</c> env var.
/// Renaming the product is an edit here + the frontend file + the public domain.
/// </summary>
public class BrandingOptions
{
    public const string SectionName = "Branding";

    public string ProductName { get; set; } = "ContractCanary";
    public string Tagline { get; set; } = "Never miss a federal contract again.";
    public string SupportEmail { get; set; } = "support@contract-canary.com";
    public string FromName { get; set; } = "ContractCanary";
    public string FromEmail { get; set; } = "digests@contract-canary.com";

    /// <summary>Public base URL of the web app, used to build links in emails.</summary>
    public string WebBaseUrl { get; set; } = "http://localhost:5173";

    public string CompanyLegalName { get; set; } = "Contract Canary";
    public string BrandColor { get; set; } = "#1d4ed8";
}
