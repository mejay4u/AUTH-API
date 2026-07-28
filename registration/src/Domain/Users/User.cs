namespace Registration.Domain.Users;

/// <summary>
/// A registered portal user. Lives in the registration service's OWN database. The email address is
/// the username / User ID. Passwords are stored only as a salted hash — never in plaintext.
/// Created by <c>completeRegistration</c> once eligibility has been confirmed, carrying the personal
/// information gathered earlier plus the subscriber and plan the member was matched to.
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

    // --- Personal information (registration screen) ---
    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public DateOnly DateOfBirth { get; set; }

    public string ZipCode { get; set; } = string.Empty;

    /// <summary>Optional; not collected by the current registration flow.</summary>
    public string? ContactNumber { get; set; }

    // --- Eligibility (matched against Facets at completion) ---

    /// <summary>The subscriber this member belongs to — mapped into the session JWT's custom claims.</summary>
    public string? SubscriberId { get; set; }

    /// <summary>The matched plan — also mapped into the session JWT's custom claims.</summary>
    public string? PlanId { get; set; }

    /// <summary>
    /// The last four digits only. The full SSN is used to match against Facets and then discarded —
    /// it is never persisted, so a database compromise cannot leak it.
    /// </summary>
    public string? SsnLast4 { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedUtc { get; set; }
}
