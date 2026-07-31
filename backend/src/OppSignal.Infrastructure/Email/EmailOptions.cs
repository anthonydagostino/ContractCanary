namespace OppSignal.Infrastructure.Email;

public class EmailOptions
{
    public const string SectionName = "Email";

    /// <summary>"Dev" (write to disk/console) or "Postmark".</summary>
    public string Provider { get; set; } = "Dev";

    /// <summary>Overrides BrandingOptions.FromEmail when set.</summary>
    public string? FromEmail { get; set; }
    public string? FromName { get; set; }

    /// <summary>Directory the Dev sender writes .html/.txt files to.</summary>
    public string DevDropPath { get; set; } = "maildrop";

    public PostmarkOptions Postmark { get; set; } = new();

    public bool IsPostmark => string.Equals(Provider, "Postmark", StringComparison.OrdinalIgnoreCase);
}

public class PostmarkOptions
{
    public string? ServerToken { get; set; }
    public string MessageStream { get; set; } = "outbound";
}
