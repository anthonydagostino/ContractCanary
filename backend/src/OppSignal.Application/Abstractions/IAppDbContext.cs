using Microsoft.EntityFrameworkCore;
using OppSignal.Domain.Entities;

namespace OppSignal.Application.Abstractions;

/// <summary>
/// Database port exposed to the Application layer. Implemented by
/// Infrastructure's <c>AppDbContext</c>. Exposes only the domain sets — Identity
/// (users, roles, refresh tokens) stays in Infrastructure.
/// </summary>
public interface IAppDbContext
{
    DbSet<MatchProfile> MatchProfiles { get; }
    DbSet<Notice> Notices { get; }
    DbSet<NoticeMatch> NoticeMatches { get; }
    DbSet<SavedNotice> SavedNotices { get; }
    DbSet<NoticeAlert> NoticeAlerts { get; }
    DbSet<Subscription> Subscriptions { get; }
    DbSet<IngestRun> IngestRuns { get; }
    DbSet<EmailLog> EmailLogs { get; }
    DbSet<Award> Awards { get; }

    DbSet<NaicsCode> NaicsCodes { get; }
    DbSet<PscCode> PscCodes { get; }
    DbSet<Agency> Agencies { get; }
    DbSet<SetAsideRef> SetAsides { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
