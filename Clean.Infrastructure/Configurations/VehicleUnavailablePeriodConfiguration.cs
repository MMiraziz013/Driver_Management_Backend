using Clean.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClassLibrary1.Configurations;

public class VehicleUnavailablePeriodConfiguration : IEntityTypeConfiguration<VehicleUnavailablePeriod>
{
    public void Configure(EntityTypeBuilder<VehicleUnavailablePeriod> builder)
    {
        // Table Name mapping
        builder.ToTable("vehicle_unavailable_periods");
        
        // Primary Key configuration
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

        // Foreign Key property mapping
        builder.Property(x => x.VehicleId)
            .HasColumnName("vehicle_id")
            .IsRequired();

        // Date properties configurations
        builder.Property(x => x.StartDate)
            .HasColumnName("start_date")
            .IsRequired();

        builder.Property(x => x.EndDate)
            .HasColumnName("end_date")
            .IsRequired();

        // Optional text properties with lengths matching your attributes
        builder.Property(x => x.Reason)
            .HasColumnName("reason")
            .HasMaxLength(100)
            .IsRequired(false);

        builder.Property(x => x.Notes)
            .HasColumnName("notes")
            .HasMaxLength(500)
            .IsRequired(false);

        // Audit property configuration
        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("CURRENT_TIMESTAMP") // Or "GETUTCDATE()" depending on your SQL engine (PostgreSQL/MySQL vs SQL Server)
            .IsRequired();

        // Relationship configuration
        // Assuming your 'Vehicle' entity has a collection property like 'ICollection<VehicleUnavailablePeriod> UnavailablePeriods'
        builder.HasOne(x => x.Vehicle)
            .WithMany(v => v.UnavailablePeriods) // Update this property name to match whatever is inside your Vehicle entity
            .HasForeignKey(x => x.VehicleId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}