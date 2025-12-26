using Clean.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClassLibrary1.Configurations;

public class UserConfigurations : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");

        builder.Property(u => u.UserName).HasMaxLength(150).IsRequired();
        builder.Property(u => u.Email).IsRequired();
        builder.Property(u => u.FirstName).HasMaxLength(150).IsRequired();
        builder.Property(u => u.LastName).HasMaxLength(150).IsRequired();
        builder.Property(u => u.Role)
            .HasConversion<string>();
    }
}