using AuthApi.Domain.Members;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AuthApi.Infrastructure.Persistence.Configurations;

public sealed class LobConfiguration : IEntityTypeConfiguration<Lob>
{
    public void Configure(EntityTypeBuilder<Lob> builder)
    {
        builder.ToTable("Lobs");
        builder.HasKey(l => l.Id);
        builder.Property(l => l.Code).HasMaxLength(64).IsRequired();
        builder.HasIndex(l => l.Code).IsUnique();
        builder.Property(l => l.Name).HasMaxLength(256).IsRequired();
    }
}
