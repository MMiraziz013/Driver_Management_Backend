using Clean.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClassLibrary1.Configurations;

public class VehicleConfigurations : IEntityTypeConfiguration<Vehicle>
{
    public void Configure(EntityTypeBuilder<Vehicle> builder)
    {
        builder.ToTable("vehicles");

        builder.Property(v => v.PlateNumber)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(v => v.Model)
            .HasMaxLength(100);

        builder.Property(v => v.RequiredDriverCategory)
            .HasConversion<string>()
            .HasMaxLength(10)
            .IsRequired();

        builder.Property(b => b.Color)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(v => v.IsActive)
            .HasDefaultValue(true);

        builder.Property(v => v.CreatedAt)
            .HasDefaultValueSql("NOW()");
        builder.Property(v => v.UpdatedAt)
            .HasDefaultValueSql("NOW()");

        builder.HasMany(v => v.Assignments)
            .WithOne(a => a.Vehicle)
            .HasForeignKey(a => a.VehicleId)
            .OnDelete(DeleteBehavior.Cascade);
        
        builder.Property(v => v.FuelTankCapacity)
            .HasDefaultValue(0);

        builder.Property(v => v.FuelConsumptionPer100Km)
            .HasDefaultValue(0);

        builder.Property(v => v.FuelType)
            .HasMaxLength(20)
            .HasDefaultValue("");

        builder.Property(v => v.InitialFuelLevel)
            .HasDefaultValue(0);

        // ===== NEW NAVIGATION PROPERTY =====

        builder.HasMany(v => v.FuelAllocations)
            .WithOne(a => a.Vehicle)
            .HasForeignKey(a => a.VehicleId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
