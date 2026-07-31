using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OppSignal.Domain.Entities;
using OppSignal.Infrastructure.Identity;

namespace OppSignal.Infrastructure.Persistence.Configurations;

public class NoticeMatchConfiguration : IEntityTypeConfiguration<NoticeMatch>
{
    public void Configure(EntityTypeBuilder<NoticeMatch> b)
    {
        b.ToTable("notice_matches");
        b.HasKey(x => x.Id);
        b.Property(x => x.MatchReason).HasColumnType("jsonb");

        b.HasOne(x => x.Notice)
            .WithMany(n => n.Matches)
            .HasForeignKey(x => x.NoticeId)
            .OnDelete(DeleteBehavior.Cascade);

        b.HasOne(x => x.MatchProfile)
            .WithMany()
            .HasForeignKey(x => x.MatchProfileId)
            .OnDelete(DeleteBehavior.Cascade);

        // Dedup guarantee: one match row per (notice, profile).
        b.HasIndex(x => new { x.NoticeId, x.MatchProfileId }).IsUnique();
        // Fast per-user digest queries for un-notified matches.
        b.HasIndex(x => new { x.UserId, x.NotifiedAt });
    }
}

public class SavedNoticeConfiguration : IEntityTypeConfiguration<SavedNotice>
{
    public void Configure(EntityTypeBuilder<SavedNotice> b)
    {
        b.ToTable("saved_notices");
        b.HasKey(x => x.Id);
        b.Property(x => x.Note).HasMaxLength(2000);

        b.HasOne(x => x.Notice)
            .WithMany()
            .HasForeignKey(x => x.NoticeId)
            .OnDelete(DeleteBehavior.Cascade);

        b.HasIndex(x => new { x.UserId, x.NoticeId }).IsUnique();
        b.HasIndex(x => x.UserId);
    }
}

public class SubscriptionConfiguration : IEntityTypeConfiguration<Subscription>
{
    public void Configure(EntityTypeBuilder<Subscription> b)
    {
        b.ToTable("subscriptions");
        b.HasKey(x => x.Id);
        b.Property(x => x.StripeCustomerId).HasMaxLength(128);
        b.Property(x => x.StripeSubscriptionId).HasMaxLength(128);
        b.Property(x => x.StripePriceId).HasMaxLength(128);

        b.HasIndex(x => x.UserId).IsUnique();
        b.HasIndex(x => x.StripeCustomerId);
        b.HasIndex(x => x.StripeSubscriptionId);
    }
}

public class IngestRunConfiguration : IEntityTypeConfiguration<IngestRun>
{
    public void Configure(EntityTypeBuilder<IngestRun> b)
    {
        b.ToTable("ingest_runs");
        b.HasKey(x => x.Id);
        b.Property(x => x.Error).HasColumnType("text");
        b.HasIndex(x => x.StartedAt);
    }
}

public class EmailLogConfiguration : IEntityTypeConfiguration<EmailLog>
{
    public void Configure(EntityTypeBuilder<EmailLog> b)
    {
        b.ToTable("email_logs");
        b.HasKey(x => x.Id);
        b.Property(x => x.ToAddress).HasMaxLength(320).IsRequired();
        b.Property(x => x.Subject).HasMaxLength(512).IsRequired();
        b.Property(x => x.Provider).HasMaxLength(32).IsRequired();
        b.Property(x => x.ProviderMessageId).HasMaxLength(256);
        b.HasIndex(x => x.SentAt);
        b.HasIndex(x => new { x.UserId, x.Kind });
    }
}

public class AwardConfiguration : IEntityTypeConfiguration<Award>
{
    public void Configure(EntityTypeBuilder<Award> b)
    {
        b.ToTable("awards");
        b.HasKey(x => x.AwardId);
        b.Property(x => x.AwardId).HasMaxLength(128);
        b.Property(x => x.RawJson).HasColumnType("jsonb");
        b.Property(x => x.ObligatedAmount).HasColumnType("numeric(18,2)");
        b.Property(x => x.PotentialTotalValue).HasColumnType("numeric(18,2)");
        b.HasIndex(x => x.PeriodOfPerformanceEnd);
        b.HasIndex(x => x.NaicsCode);
    }
}

public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> b)
    {
        b.ToTable("refresh_tokens");
        b.HasKey(x => x.Id);
        b.Property(x => x.TokenHash).HasMaxLength(128).IsRequired();
        b.Property(x => x.ReplacedByTokenHash).HasMaxLength(128);
        b.HasIndex(x => x.TokenHash).IsUnique();
        b.HasIndex(x => x.UserId);
    }
}
