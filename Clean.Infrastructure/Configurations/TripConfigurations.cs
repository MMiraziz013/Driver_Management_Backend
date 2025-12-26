using Clean.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClassLibrary1.Configurations;

public class TripConfigurations : IEntityTypeConfiguration<Trip>
{
    public void Configure(EntityTypeBuilder<Trip> builder)
    {
        builder.ToTable("trips");

        builder.Property(t => t.PickUpDate).IsRequired();
        builder.Property(t => t.GarageOutTime).IsRequired();
        builder.Property(t => t.GarageInTime).IsRequired();

        builder.Property(t => t.CompanyName)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(t => t.RoutingDetails)
            .HasMaxLength(1000);

        builder.Property(t => t.IncludedInReport)
            .HasDefaultValue(true);

        builder.HasOne(t => t.VehicleType)
            .WithMany()
            .HasForeignKey(t => t.VehicleTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(t => t.ServiceType)
            .WithMany(s => s.Trips)
            .HasForeignKey(t => t.ServiceTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(t => t.ReportPeriod)
            .WithMany(rp => rp.Trips)
            .HasForeignKey(t => t.ReportPeriodId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(t => t.Assignments)
            .WithOne(a => a.Trip)
            .HasForeignKey(a => a.TripId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
