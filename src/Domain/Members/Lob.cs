namespace AuthApi.Domain.Members;

/// <summary>
/// Line of Business. A member can be associated with more than one LOB.
/// Maps to an existing reference table in the member database.
/// </summary>
public class Lob
{
    public int Id { get; set; }

    /// <summary>Short business code emitted into the JWT (e.g. "DENTAL", "VISION").</summary>
    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public ICollection<MemberLob> MemberLobs { get; set; } = new List<MemberLob>();
}
