using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Registration.Domain.Migration;

namespace Registration.Infrastructure.Persistence.Configurations;

/// <summary>Database-first mapping of <see cref="MigrationAudit"/> onto <c>registration.MigrationAudit</c>.</summary>
public sealed class MigrationAuditConfiguration : IEntityTypeConfiguration<MigrationAudit>
{
    public void Configure(EntityTypeBuilder<MigrationAudit> builder)
    {
        builder.ToTable("MigrationAudit", "registration");

        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).ValueGeneratedNever();

        builder.Property(a => a.Email).HasMaxLength(256).IsRequired();
        builder.Property(a => a.LegacyMemberId).HasMaxLength(128);
        builder.Property(a => a.DescopeUserId).HasMaxLength(128);
        builder.Property(a => a.Success).IsRequired();
        builder.Property(a => a.Detail).HasMaxLength(256);
        builder.Property(a => a.SourceIp).HasMaxLength(45);
        builder.Property(a => a.OccurredUtc).IsRequired();

        builder.HasIndex(a => a.OccurredUtc);
    }
}
