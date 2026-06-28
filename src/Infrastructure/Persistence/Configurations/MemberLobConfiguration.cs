using AuthApi.Domain.Members;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AuthApi.Infrastructure.Persistence.Configurations;

public sealed class MemberLobConfiguration : IEntityTypeConfiguration<MemberLob>
{
    public void Configure(EntityTypeBuilder<MemberLob> builder)
    {
        builder.ToTable("MemberLobs");
        builder.HasKey(ml => new { ml.MemberId, ml.LobId });

        builder.HasOne(ml => ml.Lob)
            .WithMany(l => l.MemberLobs)
            .HasForeignKey(ml => ml.LobId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
