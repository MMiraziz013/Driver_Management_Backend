using Clean.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClassLibrary1.Configurations;

public class VehicleFuelAllocationConfigurations : IEntityTypeConfiguration<VehicleFuelAllocation>
{
    public void Configure(EntityTypeBuilder<VehicleFuelAllocation> builder)
    {
        builder.ToTable("vehicle_fuel_allocations");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.LitersAllocated)
            .IsRequired();

        builder.Property(a => a.AllocationCostUzs)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(a => a.AllocationDate)
            .IsRequired();

        builder.Property(a => a.Reason)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(a => a.Notes)
            .HasMaxLength(500);

        builder.Property(a => a.CreatedAt)
            .HasDefaultValueSql("NOW()");

        builder.Property(a => a.UpdatedAt)
            .HasDefaultValueSql("NOW()");

        // Relationships
        builder.HasOne(a => a.GasPurchase)
            .WithMany(g => g.Allocations)
            .HasForeignKey(a => a.GasPurchaseId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(a => a.Vehicle)
            .WithMany(v => v.FuelAllocations)
            .HasForeignKey(a => a.VehicleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(a => a.ReportPeriod)
            .WithMany()
            .HasForeignKey(a => a.ReportPeriodId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(a => a.Trip)
            .WithMany()
            .HasForeignKey(a => a.TripId)
            .OnDelete(DeleteBehavior.SetNull);

        // Indexes for common queries
        builder.HasIndex(a => a.VehicleId);
        builder.HasIndex(a => a.ReportPeriodId);
        builder.HasIndex(a => a.GasPurchaseId);
        builder.HasIndex(a => new { a.VehicleId, a.ReportPeriodId });
    }
}
