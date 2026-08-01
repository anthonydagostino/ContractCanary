using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using OppSignal.Application.Abstractions;
using OppSignal.Domain.Entities;
using OppSignal.Infrastructure.Identity;

namespace OppSignal.Infrastructure.Persistence;

/// <summary>
/// The single EF Core context. Extends Identity (users/roles) and implements the
/// Application <see cref="IAppDbContext"/> port for the domain sets.
/// </summary>
public class AppDbContext : IdentityDbContext<AppUser, IdentityRole<Guid>, Guid>, IAppDbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    // Domain sets (IAppDbContext)
    public DbSet<MatchProfile> MatchProfiles => Set<MatchProfile>();
    public DbSet<Notice> Notices => Set<Notice>();
    public DbSet<NoticeMatch> NoticeMatches => Set<NoticeMatch>();
    public DbSet<SavedNotice> SavedNotices => Set<SavedNotice>();
    public DbSet<NoticeAlert> NoticeAlerts => Set<NoticeAlert>();
    public DbSet<Subscription> Subscriptions => Set<Subscription>();
    public DbSet<IngestRun> IngestRuns => Set<IngestRun>();
    public DbSet<EmailLog> EmailLogs => Set<EmailLog>();
    public DbSet<Award> Awards => Set<Award>();

    public DbSet<NaicsCode> NaicsCodes => Set<NaicsCode>();
    public DbSet<PscCode> PscCodes => Set<PscCode>();
    public DbSet<Agency> Agencies => Set<Agency>();
    public DbSet<SetAsideRef> SetAsides => Set<SetAsideRef>();

    // Identity-adjacent
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
