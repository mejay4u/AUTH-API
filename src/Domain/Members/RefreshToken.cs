namespace AuthApi.Domain.Members;

/// <summary>
/// A refresh token issued alongside a short-lived access token. Only a SHA-256 hash of the token
/// is stored — the raw value is returned to the client once and never persisted, so a database
/// leak does not expose usable tokens. Tokens are single-use and rotated on every refresh.
/// </summary>
public class RefreshToken
{
    public Guid Id { get; set; }

    public Guid MemberId { get; set; }
    public Member Member { get; set; } = null!;

    /// <summary>SHA-256 hash (base64url) of the raw refresh token.</summary>
    public string TokenHash { get; set; } = string.Empty;

    public DateTime ExpiresUtc { get; set; }
    public DateTime CreatedUtc { get; set; }
    public string? CreatedByIp { get; set; }

    public DateTime? RevokedUtc { get; set; }
    public string? RevokedByIp { get; set; }

    /// <summary>Hash of the token that replaced this one — enables rotation/reuse auditing.</summary>
    public string? ReplacedByTokenHash { get; set; }

    public bool IsActive(DateTime utcNow) => RevokedUtc is null && utcNow < ExpiresUtc;

    public void Revoke(DateTime utcNow, string? ip, string? replacedByTokenHash = null)
    {
        RevokedUtc = utcNow;
        RevokedByIp = ip;
        ReplacedByTokenHash = replacedByTokenHash;
    }
}
