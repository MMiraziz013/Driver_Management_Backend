using Clean.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClassLibrary1.Data.Configurations;

public class FuelSettingsConfiguration : IEntityTypeConfiguration<FuelSettings>
{
    public void Configure(EntityTypeBuilder<FuelSettings> builder)
    {
        builder.ToTable("fuel_settings");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.DurationFallbackSpeedKmh)
            .HasDefaultValue(FuelSettings.DefaultDurationFallbackSpeedKmh);
    }
}
