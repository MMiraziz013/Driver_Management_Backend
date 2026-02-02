using Clean.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClassLibrary1.Configurations;

public class DriverPeriodStateConfiguration : IEntityTypeConfiguration<DriverPeriodState>
{
    public void Configure(EntityTypeBuilder<DriverPeriodState> builder)
    {
        builder.ToTable("driver_period_states");

        builder.HasKey(e => e.Id);
    
        builder.HasOne(e => e.Driver)
            .WithMany(d => d.PeriodStates)
            .HasForeignKey(e => e.DriverId)
            .OnDelete(DeleteBehavior.Cascade);
    
        builder.HasOne(e => e.ReportPeriod)
            .WithMany(p => p.DriverPeriodStates)
            .HasForeignKey(e => e.ReportPeriodId)
            .OnDelete(DeleteBehavior.Cascade);
    
        // Unique constraint: one state per driver per period
        builder.HasIndex(e => new { e.DriverId, e.ReportPeriodId }).IsUnique();
    
        // Index for quick lookups
        builder.HasIndex(e => e.ReportPeriodId);
        builder.HasIndex(e => e.DriverId);

    }
}