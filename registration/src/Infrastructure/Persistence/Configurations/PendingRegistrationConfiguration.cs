using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Registration.Domain.Registration;

namespace Registration.Infrastructure.Persistence.Configurations;

/// <summary>
/// Database-first mapping of <see cref="PendingRegistration"/> onto <c>registration.PendingRegistrations</c>.
/// Column names/lengths match the DDL script. The Id is application-assigned and is carried over to the
/// created user, so it must never be database-generated. Email is unique: one in-progress registration
/// per address, which is what makes the resume-on-retry path safe.
/// </summary>
public sealed class PendingRegistrationConfiguration : IEntityTypeConfiguration<PendingRegistration>
{
    public void Configure(EntityTypeBuilder<PendingRegistration> builder)
    {
        builder.ToTable("PendingRegistrations", "registration");

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).ValueGeneratedNever();

        builder.Property(p => p.Email).HasMaxLength(256).IsRequired();
        builder.Property(p => p.FirstName).HasMaxLength(100).IsRequired();
        builder.Property(p => p.LastName).HasMaxLength(100).IsRequired();
        builder.Property(p => p.DateOfBirth).IsRequired();
        builder.Property(p => p.ZipCode).HasMaxLength(10).IsRequired();

        // Null until the password step.
        builder.Property(p => p.PasswordHash).HasMaxLength(512);
        builder.Property(p => p.PasswordSalt).HasMaxLength(256);

        builder.Property(p => p.CreatedUtc).IsRequired();
        builder.Property(p => p.ExpiresUtc).IsRequired();

        // Computed from the hash columns — nothing to store.
        builder.Ignore(p => p.HasPassword);

        builder.HasIndex(p => p.Email).IsUnique();
        builder.HasIndex(p => p.ExpiresUtc);
    }
}
