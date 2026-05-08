using Clean.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClassLibrary1.Configurations;

public class AccountingReportConfiguration : IEntityTypeConfiguration<AccountingReport>
{
    public void Configure(EntityTypeBuilder<AccountingReport> builder)
    {
        builder.ToTable("accounting_reports");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Year)
            .IsRequired();

        builder.Property(r => r.Month)
            .IsRequired();

        builder.Property(r => r.TotalAmount)
            .HasColumnType("decimal(18,2)");

        builder.HasIndex(r => new { r.Year, r.Month })
            .IsUnique();

        builder.HasMany(r => r.Transactions)
            .WithOne(t => t.AccountingReport)
            .HasForeignKey(t => t.AccountingReportId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}