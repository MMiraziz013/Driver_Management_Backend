using Clean.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClassLibrary1.Data.Configurations;

public class ServiceTypeBonusConfigConfiguration : IEntityTypeConfiguration<ServiceTypeBonusConfig>
{
    public void Configure(EntityTypeBuilder<ServiceTypeBonusConfig> builder)
    {
        builder.ToTable("service_type_bonus_configs");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.CalculationMethod)
            .HasConversion<int>();

        builder.HasOne(x => x.ServiceType)
            .WithMany()
            .HasForeignKey(x => x.ServiceTypeId)
            .OnDelete(DeleteBehavior.Cascade);

        // Ensure one config per service type
        builder.HasIndex(x => x.ServiceTypeId)
            .IsUnique();
    }
}