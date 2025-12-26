using Clean.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClassLibrary1.Configurations;

public class DriverVacationConfigurations : IEntityTypeConfiguration<DriverVacation>
{
    public void Configure(EntityTypeBuilder<DriverVacation> builder)
    {
        builder.ToTable("driver_vacations");

        builder.Property(v => v.StartDate).IsRequired();
        builder.Property(v => v.EndDate).IsRequired();
        builder.Property(v => v.Notes).HasMaxLength(300);

        builder.HasOne(v => v.Driver)
            .WithMany(d => d.Vacations)
            .HasForeignKey(v => v.DriverId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
