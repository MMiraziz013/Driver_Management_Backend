using Clean.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClassLibrary1.Configurations;

public class FilterConfigurations : IEntityTypeConfiguration<Filter>
{
    public void Configure(EntityTypeBuilder<Filter> builder)
    {
        builder.ToTable("filters");

        builder.Property(f => f.Name).HasMaxLength(150).IsRequired();
        builder.Property(f => f.Description).HasMaxLength(300);

        builder.Property(f => f.Entity)
            .HasConversion<string>()
            .HasMaxLength(80)
            .IsRequired();

        builder.Property(f => f.Field).HasMaxLength(100).IsRequired();
        builder.Property(f => f.Operator)
            .HasConversion<string>()
            .HasMaxLength(80)
            .IsRequired();

        builder.Property(f => f.Value).HasMaxLength(300).IsRequired();
        builder.Property(f => f.Action)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(f => f.IsActive).HasDefaultValue(true);
        builder.Property(f => f.CreatedAt).HasDefaultValueSql("NOW()");
        builder.Property(f => f.UpdatedAt).HasDefaultValueSql("NOW()");
    }
}
