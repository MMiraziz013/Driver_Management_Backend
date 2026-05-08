using Clean.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClassLibrary1.Configurations;

public class ExchangeRateConfiguration : IEntityTypeConfiguration<ExchangeRate>
{
    public void Configure(EntityTypeBuilder<ExchangeRate> builder)
    {
        builder.ToTable("exchange_rates");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Year)
            .IsRequired();

        builder.Property(e => e.Rate)
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder.HasIndex(e => e.Year)
            .IsUnique();
    }
}