using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OppSignal.Domain.Entities;
using OppSignal.Domain.Enums;

namespace OppSignal.Infrastructure.Persistence.Configurations;

public class MatchProfileConfiguration : IEntityTypeConfiguration<MatchProfile>
{
    public void Configure(EntityTypeBuilder<MatchProfile> b)
    {
        b.ToTable("match_profiles");
        b.HasKey(x => x.Id);
        b.Property(x => x.Name).HasMaxLength(120).IsRequired();

        // String-list filters -> text[]
        b.Property(x => x.Naics).HasColumnType("text[]");
        b.Property(x => x.Psc).HasColumnType("text[]");
        b.Property(x => x.Keywords).HasColumnType("text[]");
        b.Property(x => x.AgencyPaths).HasColumnType("text[]");
        b.Property(x => x.States).HasColumnType("text[]");

        b.Property(x => x.Naics).Metadata.SetValueComparer(Converters.StringListComparer());
        b.Property(x => x.Psc).Metadata.SetValueComparer(Converters.StringListComparer());
        b.Property(x => x.Keywords).Metadata.SetValueComparer(Converters.StringListComparer());
        b.Property(x => x.AgencyPaths).Metadata.SetValueComparer(Converters.StringListComparer());
        b.Property(x => x.States).Metadata.SetValueComparer(Converters.StringListComparer());

        // Enum-list filters -> integer[]
        b.Property(x => x.SetAsides)
            .HasConversion(Converters.EnumListToIntArray<SetAsideCode>())
            .Metadata.SetValueComparer(Converters.EnumListComparer<SetAsideCode>());
        b.Property(x => x.SetAsides).HasColumnType("integer[]");

        b.Property(x => x.NoticeTypes)
            .HasConversion(Converters.EnumListToIntArray<NoticeType>())
            .Metadata.SetValueComparer(Converters.EnumListComparer<NoticeType>());
        b.Property(x => x.NoticeTypes).HasColumnType("integer[]");

        b.HasIndex(x => x.UserId);
        b.HasIndex(x => new { x.UserId, x.IsActive });
    }
}
