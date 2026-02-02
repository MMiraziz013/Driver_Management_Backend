using Clean.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClassLibrary1.Configurations;

public class CachedLocationConfigurations : IEntityTypeConfiguration<CachedLocation>
{
    public void Configure(EntityTypeBuilder<CachedLocation> builder)
    {
        builder.ToTable("cached_locations");

        builder.HasKey(e => e.Id);
        builder.HasIndex(e => e.AddressName).IsUnique();
        builder.Property(e => e.AddressName).HasMaxLength(600).IsRequired();

        builder.Property(e => e.Longitude).IsRequired();
        builder.Property(e => e.Latitude).IsRequired();
        builder.Property(e => e.CachedAt).IsRequired();
    }
}