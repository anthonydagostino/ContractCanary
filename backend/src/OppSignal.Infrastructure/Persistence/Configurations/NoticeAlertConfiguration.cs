using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OppSignal.Domain.Entities;

namespace OppSignal.Infrastructure.Persistence.Configurations;

public class NoticeAlertConfiguration : IEntityTypeConfiguration<NoticeAlert>
{
    public void Configure(EntityTypeBuilder<NoticeAlert> b)
    {
        b.ToTable("notice_alerts");
        b.HasKey(x => x.Id);
        b.Property(x => x.NoticeId).HasMaxLength(64).IsRequired();
        b.Property(x => x.Message).HasMaxLength(512).IsRequired();

        b.HasIndex(x => new { x.UserId, x.CreatedAt });
        b.HasIndex(x => new { x.UserId, x.ReadAt });

        b.HasOne(x => x.Notice)
            .WithMany()
            .HasForeignKey(x => x.NoticeId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
