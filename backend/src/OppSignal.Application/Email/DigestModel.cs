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

    /// <summary>Changes to opportunities the user is tracking (deadline moved / cancelled).</summary>
    public List<DigestAlert> Alerts { get; set; } = new();
    public int AlertCount => Alerts.Count;

    /// <summary>Tracked opportunities whose deadline is 7 / 3 / 1 days out (soonest first).</summary>
    public List<DigestClosing> ClosingSoon { get; set; } = new();
    public int ClosingSoonCount => ClosingSoon.Count;

    public bool HasMatches => TotalCount > 0;
    public bool HasAlerts => Alerts.Count > 0;
    public bool HasClosingSoon => ClosingSoon.Count > 0;

    // Branding (from BrandingOptions)
    public string ProductName { get; set; } = "OppSignal";
    public string Tagline { get; set; } = "";
    public string BrandColor { get; set; } = "#1d4ed8";
    public string SupportEmail { get; set; } = "";
    public string WebBaseUrl { get; set; } = "";
    public string SettingsUrl => $"{WebBaseUrl.TrimEnd('/')}/app/settings";
    public string DashboardUrl => $"{WebBaseUrl.TrimEnd('/')}/app";

    /// <summary>One-click unsubscribe link (CAN-SPAM) and physical postal address for the footer.</summary>
    public string UnsubscribeUrl { get; set; } = "";
    public string CompanyPostalAddress { get; set; } = "";
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

/// <summary>A change-alert line in the digest (deadline moved / opportunity cancelled).</summary>
public sealed class DigestAlert
{
    public string NoticeId { get; set; } = "";
    public string Title { get; set; } = "";
    public string TypeLabel { get; set; } = "";
    public string Message { get; set; } = "";
    public bool IsCancelled { get; set; }
    public string DetailLink { get; set; } = "";
}

/// <summary>A "closing soon" deadline-reminder line in the digest.</summary>
public sealed class DigestClosing
{
    public string NoticeId { get; set; } = "";
    public string Title { get; set; } = "";
    public string Agency { get; set; } = "";
    public string DeadlineLabel { get; set; } = "";
    public int DaysLeft { get; set; }
    public string DaysLeftLabel => DaysLeft == 1 ? "Closes tomorrow" : $"Closes in {DaysLeft} days";
    public string DetailLink { get; set; } = "";
    public string SamLink { get; set; } = "";
}
