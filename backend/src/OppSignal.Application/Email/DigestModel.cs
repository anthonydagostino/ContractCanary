namespace OppSignal.Application.Email;

/// <summary>View model for the daily digest email (grouped by profile).</summary>
public sealed class DigestModel
{
    public Guid UserId { get; set; }
    public string ToEmail { get; set; } = "";
    public string? ToName { get; set; }
    public string DateLabel { get; set; } = "";          // e.g. "Tuesday, June 16"

    public List<DigestGroup> Groups { get; set; } = new();
    public int TotalCount => Groups.Sum(g => g.Items.Count);

    // Branding (from BrandingOptions)
    public string ProductName { get; set; } = "OppSignal";
    public string Tagline { get; set; } = "";
    public string BrandColor { get; set; } = "#1d4ed8";
    public string SupportEmail { get; set; } = "";
    public string WebBaseUrl { get; set; } = "";
    public string SettingsUrl => $"{WebBaseUrl.TrimEnd('/')}/app/settings";
    public string DashboardUrl => $"{WebBaseUrl.TrimEnd('/')}/app";
}

public sealed class DigestGroup
{
    public Guid ProfileId { get; set; }
    public string ProfileName { get; set; } = "";
    public List<DigestItem> Items { get; set; } = new();
}

public sealed class DigestItem
{
    public string NoticeId { get; set; } = "";
    public string Title { get; set; } = "";
    public string Agency { get; set; } = "";
    public string TypeLabel { get; set; } = "";
    public string? Deadline { get; set; }
    public string? SetAside { get; set; }
    public string? PlaceOfPerformance { get; set; }
    public string SamLink { get; set; } = "";
    public string DetailLink { get; set; } = "";
}
