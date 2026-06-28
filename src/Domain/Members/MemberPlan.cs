namespace AuthApi.Domain.Members;

/// <summary>Join entity for the many-to-many relationship between members and benefit plans.</summary>
public class MemberPlan
{
    public Guid MemberId { get; set; }
    public Member Member { get; set; } = null!;

    public int PlanId { get; set; }
    public Plan Plan { get; set; } = null!;
}
