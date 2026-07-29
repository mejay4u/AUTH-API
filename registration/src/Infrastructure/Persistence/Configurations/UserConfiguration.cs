using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Registration.Domain.Users;

namespace Registration.Infrastructure.Persistence.Configurations;

/// <summary>
/// Database-first mapping of <see cref="User"/> onto the <c>registration.Users</c> table. Column names,
/// lengths, keys and the unique indexes match the DDL script exactly — change the SQL and this mapping
/// together. The Id is application-assigned (carried over from the pending record), not DB-generated.
/// </summary>
public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users", "registration");

        builder.HasKey(u => u.Id);
        builder.Property(u => u.Id).ValueGeneratedNever();

        builder.Property(u => u.Email).HasMaxLength(256).IsRequired();
        builder.Property(u => u.Username).HasMaxLength(256).IsRequired();
        builder.Property(u => u.PasswordHash).HasMaxLength(512).IsRequired();
        builder.Property(u => u.PasswordSalt).HasMaxLength(256).IsRequired();
        builder.Property(u => u.DescopeUserId).HasMaxLength(64);

        // Personal information (registration screen).
        builder.Property(u => u.FirstName).HasMaxLength(100).IsRequired();
        builder.Property(u => u.LastName).HasMaxLength(100).IsRequired();
        builder.Property(u => u.DateOfBirth).IsRequired();               // maps to SQL 'date'
        builder.Property(u => u.ZipCode).HasMaxLength(10).IsRequired();
        builder.Property(u => u.ContactNumber).HasMaxLength(20);         // nullable (optional)

        builder.Property(u => u.IsActive).IsRequired();
        builder.Property(u => u.CreatedUtc).IsRequired();

        // Enforce uniqueness at the database — the real duplicate-email guard.
        builder.HasIndex(u => u.Email).IsUnique();
        builder.HasIndex(u => u.Username).IsUnique();
    }
}
