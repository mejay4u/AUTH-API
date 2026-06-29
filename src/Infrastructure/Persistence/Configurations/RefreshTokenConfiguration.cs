using AuthApi.Domain.Members;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AuthApi.Infrastructure.Persistence.Configurations;

public sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    /// <summary>
    /// Schema that owns the refresh-token table. Kept separate from the legacy user table (dbo) so the
    /// Auth API fully owns its persistence. Change in one place if your auth schema/DB name differs.
    /// </summary>
    public const string Schema = "auth";

    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("RefreshTokens", Schema);
        builder.HasKey(rt => rt.Id);

        builder.Property(rt => rt.TokenHash).HasMaxLength(256).IsRequired();
        builder.HasIndex(rt => rt.TokenHash).IsUnique();

        builder.Property(rt => rt.Lob).HasMaxLength(64).IsRequired();

        builder.Property(rt => rt.CreatedByIp).HasMaxLength(64);
        builder.Property(rt => rt.RevokedByIp).HasMaxLength(64);
        builder.Property(rt => rt.ReplacedByTokenHash).HasMaxLength(256);

        // Indexed for member-scoped lookups (e.g. revoke-all on reuse detection).
        // No foreign key to the user table by design — this table is decoupled from legacy data.
        builder.HasIndex(rt => rt.MemberId);
    }
}
