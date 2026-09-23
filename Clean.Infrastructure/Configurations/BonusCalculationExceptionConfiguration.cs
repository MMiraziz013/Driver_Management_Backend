using Clean.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClassLibrary1.Data.Configurations;

public class BonusCalculationExceptionConfiguration : IEntityTypeConfiguration<BonusCalculationException>
{
    public void Configure(EntityTypeBuilder<BonusCalculationException> builder)
    {
        builder.ToTable("bonus_calculation_exceptions");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.CompanyNameContains)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.CalculationMethod)
            .HasConversion<int>();

        builder.Property(x => x.VehicleTypesJson)
            .HasColumnName("vehicle_types")
            .HasColumnType("text");

        builder.Property(x => x.LocationKeywordsJson)
            .HasColumnName("location_keywords")
            .HasColumnType("text");

        builder.HasOne(x => x.ServiceType)
            .WithMany()
            .HasForeignKey(x => x.ServiceTypeId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
