using Clean.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClassLibrary1.Configurations;

public class DriverAssignmentConfigurations : IEntityTypeConfiguration<DriverAssignment>
{
    public void Configure(EntityTypeBuilder<DriverAssignment> builder)
    {
        builder.ToTable("driver_assignments");

        builder.Property(d => d.DriverId).IsRequired(false);
        builder.Property(d => d.VehicleId).IsRequired(false);  // Add this line
        
        builder.Property(a => a.AssignmentType)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(a => a.HasConflict)
            .HasDefaultValue(false);

        builder.Property(a => a.Notes)
            .HasMaxLength(300);

        builder.HasOne(a => a.Driver)
            .WithMany(d => d.Assignments)
            .HasForeignKey(a => a.DriverId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(a => a.Trip)
            .WithMany(t => t.Assignments)
            .HasForeignKey(a => a.TripId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(a => a.Vehicle)
            .WithMany(v => v.Assignments)
            .HasForeignKey(a => a.VehicleId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}