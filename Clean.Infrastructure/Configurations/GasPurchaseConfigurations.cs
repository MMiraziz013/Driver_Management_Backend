using Clean.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClassLibrary1.Configurations;

public class GasPurchaseConfigurations : IEntityTypeConfiguration<GasPurchase>
{
    public void Configure(EntityTypeBuilder<GasPurchase> builder)
    {
        builder.ToTable("gas_purchases");

        builder.HasKey(g => g.Id);

        builder.Property(g => g.PurchaseDate)
            .IsRequired();

        builder.Property(g => g.LitersAmount)
            .IsRequired();

        builder.Property(g => g.FuelType)
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(g => g.AmountUzs)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(g => g.AllocatedLiters)
            .HasDefaultValue(0);

        builder.Property(g => g.Notes)
            .HasMaxLength(500);

        builder.Property(g => g.CreatedAt)
            .HasDefaultValueSql("NOW()");

        builder.Property(g => g.UpdatedAt)
            .HasDefaultValueSql("NOW()");

        // Ignore computed properties
        builder.Ignore(g => g.PricePerLiter);
        builder.Ignore(g => g.RemainingLiters);
        builder.Ignore(g => g.IsFullyAllocated);

        // Relationships
        builder.HasOne(g => g.ReportPeriod)
            .WithMany()
            .HasForeignKey(g => g.ReportPeriodId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(g => g.Allocations)
            .WithOne(a => a.GasPurchase)
            .HasForeignKey(a => a.GasPurchaseId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes for common queries
        builder.HasIndex(g => g.ReportPeriodId);
        builder.HasIndex(g => g.FuelType);
        builder.HasIndex(g => g.PurchaseDate);
    }
}
