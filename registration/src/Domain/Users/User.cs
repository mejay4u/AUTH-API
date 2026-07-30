namespace Registration.Domain.Users;

/// <summary>
/// A registered portal user. Lives in the registration service's OWN database. The email address is
/// the username / User ID. Passwords are stored only as a salted hash — never in plaintext.
/// Captures the personal information collected on the registration screen.
/// </summary>
public class User
{
    /// <summary>Same id as the <c>PendingRegistration</c> it was promoted from.</summary>
    public Guid Id { get; set; }

    public string Email { get; set; } = string.Empty;

    /// <summary>Login identifier — equal to <see cref="Email"/>.</summary>
    public string Username { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public string PasswordSalt { get; set; } = string.Empty;

    /// <summary>
    /// The Descope user id, carried over from the pending record — the durable link between the
    /// Descope identity and this member, for exchanging a validated Descope token for the Auth API's
    /// own enriched one. Currently always null: the flow creates its shadow record only *after*
    /// <c>initiateRegistration</c> returns, so there is no id to record at that point. Kept because a
    /// later step can populate it; until then the link is the email address.
    /// </summary>
    public string? DescopeUserId { get; set; }

    // --- Personal information (registration screen) ---
    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public DateOnly DateOfBirth { get; set; }

    public string ZipCode { get; set; } = string.Empty;

    /// <summary>Optional — the only non-required field on the screen.</summary>
    public string? ContactNumber { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedUtc { get; set; }
}
