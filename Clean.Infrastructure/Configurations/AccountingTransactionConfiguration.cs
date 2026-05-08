using Clean.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClassLibrary1.Configurations;

public class AccountingTransactionConfiguration : IEntityTypeConfiguration<AccountingTransaction>
{
    public void Configure(EntityTypeBuilder<AccountingTransaction> builder)
    {
        builder.ToTable("accounting_transactions");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Year)
            .IsRequired();

        builder.Property(t => t.Month)
            .IsRequired();

        builder.Property(t => t.Type)
            .IsRequired();

        builder.Property(t => t.TripTotal)
            .HasColumnType("decimal(18,2)");

        builder.HasIndex(t => new { t.Year, t.Month });
        builder.HasIndex(t => t.Type);
        builder.HasIndex(t => t.Company);
        builder.HasIndex(t => t.Car);
    }
}