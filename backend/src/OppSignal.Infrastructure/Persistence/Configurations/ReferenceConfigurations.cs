using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OppSignal.Domain.Entities;

namespace OppSignal.Infrastructure.Persistence.Configurations;

public class NaicsCodeConfiguration : IEntityTypeConfiguration<NaicsCode>
{
    public void Configure(EntityTypeBuilder<NaicsCode> b)
    {
        b.ToTable("naics_codes");
        b.HasKey(x => x.Code);
        b.Property(x => x.Code).HasMaxLength(6);
        b.Property(x => x.Title).HasMaxLength(512).IsRequired();
        b.Property(x => x.ParentCode).HasMaxLength(6);
        b.HasIndex(x => x.Level);
    }
}

public class PscCodeConfiguration : IEntityTypeConfiguration<PscCode>
{
    public void Configure(EntityTypeBuilder<PscCode> b)
    {
        b.ToTable("psc_codes");
        b.HasKey(x => x.Code);
        b.Property(x => x.Code).HasMaxLength(8);
        b.Property(x => x.Title).HasMaxLength(512).IsRequired();
        b.Property(x => x.Category).HasMaxLength(128).IsRequired();
    }
}

public class AgencyConfiguration : IEntityTypeConfiguration<Agency>
{
    public void Configure(EntityTypeBuilder<Agency> b)
    {
        b.ToTable("agencies");
        b.HasKey(x => x.Code);
        b.Property(x => x.Code).HasMaxLength(16);
        b.Property(x => x.Name).HasMaxLength(256).IsRequired();
        b.Property(x => x.ParentCode).HasMaxLength(16);
        b.HasIndex(x => x.Tier);
    }
}

public class SetAsideRefConfiguration : IEntityTypeConfiguration<SetAsideRef>
{
    public void Configure(EntityTypeBuilder<SetAsideRef> b)
    {
        b.ToTable("set_asides");
        b.HasKey(x => x.Code);
        b.Property(x => x.Code).HasMaxLength(16);
        b.Property(x => x.Name).HasMaxLength(128).IsRequired();
        b.Property(x => x.Description).HasMaxLength(512).IsRequired();
    }
}
