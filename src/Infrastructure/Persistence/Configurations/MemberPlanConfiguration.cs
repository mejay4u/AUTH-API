using AuthApi.Domain.Members;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AuthApi.Infrastructure.Persistence.Configurations;

public sealed class MemberPlanConfiguration : IEntityTypeConfiguration<MemberPlan>
{
    public void Configure(EntityTypeBuilder<MemberPlan> builder)
    {
        builder.ToTable("MemberPlans");
        builder.HasKey(mp => new { mp.MemberId, mp.PlanId });

        builder.HasOne(mp => mp.Plan)
            .WithMany(p => p.MemberPlans)
            .HasForeignKey(mp => mp.PlanId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
