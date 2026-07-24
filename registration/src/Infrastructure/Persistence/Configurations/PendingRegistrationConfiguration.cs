using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Registration.Domain.Registration;

namespace Registration.Infrastructure.Persistence.Configurations;

/// <summary>
/// Database-first mapping of <see cref="PendingRegistration"/> onto <c>registration.PendingRegistrations</c>.
/// Column names/lengths match the DDL script. The Id (the registrationId) is application-assigned.
/// An index on ExpiresUtc supports cleanup of abandoned sessions.
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
        builder.Property(p => p.ContactNumber).HasMaxLength(20);
        builder.Property(p => p.EmailVerified).IsRequired();
        builder.Property(p => p.CreatedUtc).IsRequired();
        builder.Property(p => p.ExpiresUtc).IsRequired();

        builder.HasIndex(p => p.ExpiresUtc);
    }
}
