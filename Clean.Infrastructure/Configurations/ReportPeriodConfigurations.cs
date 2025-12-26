using Clean.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClassLibrary1.Configurations;

public class ReportPeriodConfigurations : IEntityTypeConfiguration<ReportPeriod>
{
    public void Configure(EntityTypeBuilder<ReportPeriod> builder)
    {
        builder.ToTable("report_periods");

        builder.Property(rp => rp.StartDate).IsRequired();
        builder.Property(rp => rp.EndDate).IsRequired();
        builder.Property(rp => rp.Description).HasMaxLength(300);
        builder.Property(rp => rp.GeneratedAt).HasDefaultValueSql("NOW()");
        builder.Property(rp => rp.GeneratedBy).HasMaxLength(150);

        builder.Property(rp => rp.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.HasMany(rp => rp.Trips)
            .WithOne(t => t.ReportPeriod)
            .HasForeignKey(t => t.ReportPeriodId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
