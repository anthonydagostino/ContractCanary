using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OppSignal.Domain.Entities;

namespace OppSignal.Infrastructure.Persistence.Configurations;

public class NoticeConfiguration : IEntityTypeConfiguration<Notice>
{
    public void Configure(EntityTypeBuilder<Notice> b)
    {
        b.ToTable("notices");
        b.HasKey(x => x.NoticeId);
        b.Property(x => x.NoticeId).HasMaxLength(64);
        b.Property(x => x.Title).HasMaxLength(1024).IsRequired();
        b.Property(x => x.SolicitationNumber).HasMaxLength(256);
        b.Property(x => x.AgencyPath).HasMaxLength(1024);
        b.Property(x => x.DepartmentName).HasMaxLength(512);
        b.Property(x => x.SubTierName).HasMaxLength(512);
        b.Property(x => x.OfficeName).HasMaxLength(512);
        b.Property(x => x.NaicsCode).HasMaxLength(12);
        b.Property(x => x.PscCode).HasMaxLength(12);
        // Usually a 2-letter US state code, but real SAM data carries longer values
        // (international provinces, or malformed records with a full name in the code
        // field). Kept generous so a single messy record can't abort the ingest batch.
        b.Property(x => x.PopState).HasMaxLength(16);
        b.Property(x => x.PopCountry).HasMaxLength(8);
        b.Property(x => x.RawJson).HasColumnType("jsonb");

        b.HasIndex(x => x.PostedDate);
        b.HasIndex(x => x.ResponseDeadline);
        b.HasIndex(x => x.NaicsCode);
        b.HasIndex(x => x.PscCode);
        b.HasIndex(x => x.SetAside);
        b.HasIndex(x => x.Type);
        b.HasIndex(x => x.PopState);
    }
}
