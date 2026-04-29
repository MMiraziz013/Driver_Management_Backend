using Clean.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClassLibrary1.Data.Configurations;

public class BonusSettingsConfiguration : IEntityTypeConfiguration<BonusSettings>
{
    public void Configure(EntityTypeBuilder<BonusSettings> builder)
    {
        builder.ToTable("bonus_settings");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.QuantityPremiumVehicleRate)
            .HasColumnType("decimal(18,2)");

        builder.Property(x => x.QuantityStandardVehicleRate)
            .HasColumnType("decimal(18,2)");

        builder.Property(x => x.RoundTripPremiumVehicleRate)
            .HasColumnType("decimal(18,2)");

        builder.Property(x => x.RoundTripStandardVehicleRate)
            .HasColumnType("decimal(18,2)");

        builder.Property(x => x.DurationUnder2HoursRate)
            .HasColumnType("decimal(18,2)");

        builder.Property(x => x.DurationUnder4HoursRate)
            .HasColumnType("decimal(18,2)");

        builder.Property(x => x.Duration4To6HoursRate)
            .HasColumnType("decimal(18,2)");

        builder.Property(x => x.Duration6To8HoursRate)
            .HasColumnType("decimal(18,2)");
        
        builder.Property(x => x.Duration8To10HoursRate)
            .HasColumnType("decimal(18,2)");
        
        builder.Property(x => x.Duration10To12HoursRate)
            .HasColumnType("decimal(18,2)");

        builder.Property(x => x.Duration12To14HoursRate)
            .HasColumnType("decimal(18,2)");
        
        builder.Property(x => x.DurationOver14HoursRate)
            .HasColumnType("decimal(18,2)");

        builder.Property(x => x.FieldTripDailyRate)
            .HasColumnType("decimal(18,2)");

        builder.Property(x => x.PremiumVehicleTypesJson)
            .HasColumnName("premium_vehicle_types")
            .HasColumnType("text");
    }
}