namespace AuthApi.Domain.Members;

/// <summary>Join entity for the many-to-many relationship between members and lines of business.</summary>
public class MemberLob
{
    public Guid MemberId { get; set; }
    public Member Member { get; set; } = null!;

    public int LobId { get; set; }
    public Lob Lob { get; set; } = null!;
}
