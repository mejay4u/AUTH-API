using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Registration.Domain.Users;

namespace Registration.Infrastructure.Persistence.Configurations;

/// <summary>
/// Database-first mapping of <see cref="User"/> onto <c>registration.Users</c> (no password columns).
/// Email/username are unique; the Descope id is uniquely indexed only when present (migrated users have
/// none until Descope links them).
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
        builder.Property(u => u.FirstName).HasMaxLength(100).IsRequired();
        builder.Property(u => u.LastName).HasMaxLength(100).IsRequired();
        builder.Property(u => u.DateOfBirth);                       // nullable date
        builder.Property(u => u.ZipCode).HasMaxLength(10);          // nullable
        builder.Property(u => u.ContactNumber).HasMaxLength(20);    // nullable
        builder.Property(u => u.DescopeUserId).HasMaxLength(128);   // nullable
        builder.Property(u => u.Origin).HasMaxLength(20).IsRequired();
        builder.Property(u => u.LegacyMemberId).HasMaxLength(128);  // nullable
        builder.Property(u => u.IsActive).IsRequired();
        builder.Property(u => u.CreatedUtc).IsRequired();
        builder.Property(u => u.UpdatedUtc).IsRequired();
        builder.Property(u => u.MigratedUtc);                       // nullable

        builder.HasIndex(u => u.Email).IsUnique();
        builder.HasIndex(u => u.Username).IsUnique();
        builder.HasIndex(u => u.DescopeUserId)
            .IsUnique()
            .HasFilter("[DescopeUserId] IS NOT NULL");              // ignored by the InMemory provider
    }
}
