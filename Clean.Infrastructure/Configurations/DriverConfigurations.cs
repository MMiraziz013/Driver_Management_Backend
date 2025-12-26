using Clean.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClassLibrary1.Configurations;

public class DriverConfigurations : IEntityTypeConfiguration<Driver>
{
    public void Configure(EntityTypeBuilder<Driver> builder)
    {
        builder.ToTable("drivers");

        builder.Property(d => d.FullName)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(d => d.Category)
            .HasConversion<string>()
            .HasMaxLength(10)
            .IsRequired();

        builder.Property(d => d.EmploymentType)
            .HasConversion<string>()
            .HasMaxLength(10)
            .IsRequired();
        
        builder.Property(d => d.BirthDay)
            .HasConversion(
                v => v.ToDateTime(TimeOnly.MinValue), // Convert to DateTime when saving
                v => DateOnly.FromDateTime(v)         // Convert to DateOnly when reading
            )
            .HasColumnType("date"); // Use 'date' instead of 'datetime' in SQL

        builder.Ignore(d => d.Age);
        
        builder.Property(d => d.Address)
            .HasMaxLength(250);

        builder.Property(d => d.WeeklyWorkLimit)
            .HasDefaultValue(5);

        builder.Property(d => d.IsActive)
            .HasDefaultValue(true);

        builder.Property(d => d.CreatedAt)
            .HasDefaultValueSql("NOW()");
        builder.Property(d => d.UpdatedAt)
            .HasDefaultValueSql("NOW()");

        builder.HasMany(d => d.Vacations)
            .WithOne(v => v.Driver)
            .HasForeignKey(v => v.DriverId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(d => d.OffDays)
            .WithOne(o => o.Driver)
            .HasForeignKey(o => o.DriverId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(d => d.Assignments)
            .WithOne(a => a.Driver)
            .HasForeignKey(a => a.DriverId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
