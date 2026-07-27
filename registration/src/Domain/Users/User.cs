namespace Registration.Domain.Users;

/// <summary>
/// A portal user record — the profile/identity system of record. Descope owns authentication and the
/// password; this record holds profile data and the mapping to the Descope identity (and, for migrated
/// legacy users, back to the member portal record). No password material is ever stored here.
/// </summary>
public class User
{
    public Guid Id { get; set; }

    public string Email { get; set; } = string.Empty;

    /// <summary>Login identifier — equal to <see cref="Email"/>.</summary>
    public string Username { get; set; } = string.Empty;

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public DateOnly? DateOfBirth { get; set; }

    public string? ZipCode { get; set; }

    public string? ContactNumber { get; set; }

    /// <summary>
    /// The Descope subject id — the link to the Descope identity. Null for a JIT-migrated user until
    /// Descope assigns/links one (set later via the sync webhook).
    /// </summary>
    public string? DescopeUserId { get; set; }

    /// <summary><see cref="UserOrigin"/> — how the record was created.</summary>
    public string Origin { get; set; } = UserOrigin.Registration;

    /// <summary>Link back to the legacy member portal record (set for migrated users).</summary>
    public string? LegacyMemberId { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedUtc { get; set; }

    public DateTime UpdatedUtc { get; set; }

    /// <summary>When a legacy user was JIT-migrated (null for self-service registrations).</summary>
    public DateTime? MigratedUtc { get; set; }
}
