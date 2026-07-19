using Clean.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClassLibrary1.Configurations;

public class CompanyCategoryConfiguration : IEntityTypeConfiguration<CompanyCategory>
{
    public void Configure(EntityTypeBuilder<CompanyCategory> builder)
    {
        builder.ToTable("company_categories");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.HasIndex(c => c.Name)
            .IsUnique();

        builder.HasMany(c => c.Companies)
            .WithOne(co => co.Category)
            .HasForeignKey(co => co.CompanyCategoryId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}