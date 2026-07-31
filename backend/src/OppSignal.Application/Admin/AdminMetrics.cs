namespace OppSignal.Application.Admin;

public sealed class AdminMetricsDto
{
    public int TotalUsers { get; set; }
    public int AdminUsers { get; set; }

    public int ActiveSubscribers { get; set; }
    public int Trialing { get; set; }
    public int StarterSubscribers { get; set; }
    public int ProSubscribers { get; set; }
    public int PastDue { get; set; }
    public int Canceled { get; set; }

    public int TotalNotices { get; set; }
    public int ActiveNotices { get; set; }
    public int NoticesLast24h { get; set; }

    public int TotalMatches { get; set; }

    public int EmailsSentTotal { get; set; }
    public int EmailsSentLast24h { get; set; }
    public int EmailFailuresLast24h { get; set; }

    public LastIngestRunDto? LastIngestRun { get; set; }
}

public sealed class LastIngestRunDto
{
    public string Source { get; set; } = "";
    public string Status { get; set; } = "";
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public int NoticesInserted { get; set; }
    public int NoticesUpdated { get; set; }
    public int MatchesCreated { get; set; }
    public string? Error { get; set; }
}

public interface IAdminMetricsService
{
    Task<AdminMetricsDto> GetAsync(CancellationToken ct = default);
}
