using Microsoft.EntityFrameworkCore;
using OppSignal.Application.Abstractions;
using OppSignal.Application.Admin;
using OppSignal.Domain.Enums;
using OppSignal.Infrastructure.Persistence;

namespace OppSignal.Infrastructure.Admin;

/// <summary>
/// Admin dashboard metrics. Lives in Infrastructure because it also counts
/// Identity users (not exposed by the Application db port).
/// </summary>
public sealed class AdminMetricsService : IAdminMetricsService
{
    private readonly AppDbContext _db;
    private readonly IClock _clock;

    public AdminMetricsService(AppDbContext db, IClock clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task<AdminMetricsDto> GetAsync(CancellationToken ct = default)
    {
        var since = _clock.UtcNow.AddHours(-24);

        var m = new AdminMetricsDto
        {
            TotalUsers = await _db.Users.CountAsync(ct),
            AdminUsers = await _db.Users.CountAsync(u => u.IsAdmin, ct),

            ActiveSubscribers = await _db.Subscriptions.CountAsync(s => s.Status == SubscriptionStatus.Active, ct),
            Trialing = await _db.Subscriptions.CountAsync(s => s.Status == SubscriptionStatus.Trialing, ct),
            StarterSubscribers = await _db.Subscriptions.CountAsync(
                s => s.Plan == PlanTier.Starter && (s.Status == SubscriptionStatus.Active || s.Status == SubscriptionStatus.Trialing), ct),
            ProSubscribers = await _db.Subscriptions.CountAsync(
                s => s.Plan == PlanTier.Pro && (s.Status == SubscriptionStatus.Active || s.Status == SubscriptionStatus.Trialing), ct),
            PastDue = await _db.Subscriptions.CountAsync(s => s.Status == SubscriptionStatus.PastDue, ct),
            Canceled = await _db.Subscriptions.CountAsync(s => s.Status == SubscriptionStatus.Canceled, ct),

            TotalNotices = await _db.Notices.CountAsync(ct),
            ActiveNotices = await _db.Notices.CountAsync(n => n.IsActive, ct),
            NoticesLast24h = await _db.Notices.CountAsync(n => n.FirstSeenAt >= since, ct),

            TotalMatches = await _db.NoticeMatches.CountAsync(ct),

            EmailsSentTotal = await _db.EmailLogs.CountAsync(e => e.Success, ct),
            EmailsSentLast24h = await _db.EmailLogs.CountAsync(e => e.Success && e.SentAt >= since, ct),
            EmailFailuresLast24h = await _db.EmailLogs.CountAsync(e => !e.Success && e.SentAt >= since, ct),
        };

        var last = await _db.IngestRuns.OrderByDescending(r => r.StartedAt).FirstOrDefaultAsync(ct);
        if (last is not null)
        {
            m.LastIngestRun = new LastIngestRunDto
            {
                Source = last.Source.ToString(),
                Status = last.Status.ToString(),
                StartedAt = last.StartedAt,
                CompletedAt = last.CompletedAt,
                NoticesInserted = last.NoticesInserted,
                NoticesUpdated = last.NoticesUpdated,
                MatchesCreated = last.MatchesCreated,
                Error = last.Error,
            };
        }

        return m;
    }
}
