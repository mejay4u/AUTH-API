namespace Registration.Domain.Migration;

/// <summary>
/// An append-only record of a JIT (lazy) migration attempt for a legacy member. Written on every
/// verify-hook call so migrations are auditable (who, outcome, when).
/// </summary>
public class MigrationAudit
{
    public Guid Id { get; set; }

    public string Email { get; set; } = string.Empty;

    public string? LegacyMemberId { get; set; }

    public string? DescopeUserId { get; set; }

    public bool Success { get; set; }

    /// <summary>Short outcome detail (no secrets/PII beyond the email already recorded).</summary>
    public string? Detail { get; set; }

    public string? SourceIp { get; set; }

    public DateTime OccurredUtc { get; set; }
}
