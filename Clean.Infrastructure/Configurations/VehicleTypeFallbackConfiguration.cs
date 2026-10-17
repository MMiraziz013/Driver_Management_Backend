using Clean.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClassLibrary1.Configurations;

public class VehicleTypeFallbackConfiguration : IEntityTypeConfiguration<VehicleTypeFallback>
{
    public void Configure(EntityTypeBuilder<VehicleTypeFallback> builder)
    {
        builder.ToTable("vehicle_type_fallbacks");

        builder.HasKey(x => x.Id);

        builder.HasIndex(x => new { x.VehicleTypeId, x.FallbackVehicleTypeId })
            .IsUnique();

        builder.HasOne(x => x.VehicleType)
            .WithMany(vt => vt.Fallbacks)
            .HasForeignKey(x => x.VehicleTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.FallbackVehicleType)
            .WithMany()
            .HasForeignKey(x => x.FallbackVehicleTypeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
