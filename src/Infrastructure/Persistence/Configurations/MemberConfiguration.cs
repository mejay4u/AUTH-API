using AuthApi.Domain.Members;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AuthApi.Infrastructure.Persistence.Configurations;

/// <summary>
/// Maps <see cref="Member"/> to the existing member table. Adjust table/column names here to match
/// the real schema when pointing at SQL Server — nothing else needs to change.
/// </summary>
public sealed class MemberConfiguration : IEntityTypeConfiguration<Member>
{
    public void Configure(EntityTypeBuilder<Member> builder)
    {
        builder.ToTable("Members");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.Username).HasMaxLength(256).IsRequired();
        builder.HasIndex(m => m.Username).IsUnique();

        builder.Property(m => m.PasswordHash).HasMaxLength(512).IsRequired();
        builder.Property(m => m.PasswordSalt).HasMaxLength(512).IsRequired();

        builder.Property(m => m.Email).HasMaxLength(256);
        builder.Property(m => m.FirstName).HasMaxLength(128);
        builder.Property(m => m.LastName).HasMaxLength(128);

        builder.Property(m => m.IsActive).HasDefaultValue(true);
        builder.Property(m => m.AccessFailedCount).HasDefaultValue(0);

        builder.HasMany(m => m.MemberLobs)
            .WithOne(ml => ml.Member)
            .HasForeignKey(ml => ml.MemberId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(m => m.MemberPlans)
            .WithOne(mp => mp.Member)
            .HasForeignKey(mp => mp.MemberId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
