using Clean.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClassLibrary1.Configurations;

public class DriverOffDayConfigurations : IEntityTypeConfiguration<DriverOffDay>
{
    public void Configure(EntityTypeBuilder<DriverOffDay> builder)
    {
        builder.ToTable("driver_off_days");

        builder.Property(o => o.Date).IsRequired();
        builder.Property(o => o.Reason).HasMaxLength(300);

        builder.HasOne(o => o.Driver)
            .WithMany(d => d.OffDays)
            .HasForeignKey(o => o.DriverId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
