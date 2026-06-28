namespace AuthApi.Domain.Members;

/// <summary>
/// A benefit plan. A member can be enrolled in more than one plan, and a plan belongs to a LOB.
/// Maps to an existing reference table in the member database.
/// </summary>
public class Plan
{
    public int Id { get; set; }

    /// <summary>Short plan code (e.g. "PPO-1000").</summary>
    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    /// <summary>Optional owning line of business.</summary>
    public int? LobId { get; set; }
    public Lob? Lob { get; set; }

    public ICollection<MemberPlan> MemberPlans { get; set; } = new List<MemberPlan>();
}
