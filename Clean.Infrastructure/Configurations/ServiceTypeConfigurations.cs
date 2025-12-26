using Clean.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClassLibrary1.Configurations;

public class ServiceTypeConfigurations : IEntityTypeConfiguration<ServiceType>
{
    public void Configure(EntityTypeBuilder<ServiceType> builder)
    {
        builder.ToTable("service_types");

        builder.Property(s => s.Name)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(s => s.Description)
            .HasMaxLength(300);

        builder.HasMany(s => s.Trips)
            .WithOne(t => t.ServiceType)
            .HasForeignKey(t => t.ServiceTypeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
