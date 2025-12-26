using Clean.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClassLibrary1.Configurations;

public class VehicleTypeConfigurations : IEntityTypeConfiguration<VehicleType>
{
    public void Configure(EntityTypeBuilder<VehicleType> builder)
    {
        builder.ToTable("vehicle_types");

        builder.Property(vt => vt.Name)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(vt => vt.Description)
            .HasMaxLength(300)
            .IsRequired(false);

        builder.HasMany(vt => vt.Vehicles)
            .WithOne(v => v.VehicleType)
            .HasForeignKey(v => v.VehicleTypeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
