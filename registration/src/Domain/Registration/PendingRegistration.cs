namespace Registration.Domain.Registration;

/// <summary>
/// A member's registration while it is in progress — the "Pending" state. Created by
/// <c>initiateRegistration</c> once Descope has verified the email, and promoted to a
/// <see cref="Users.User"/> when the account is created, at which point it is deleted.
/// Lives in the registration database so the server, not the client, is the source of truth for the
/// details that were reviewed.
/// </summary>
/// <remarks>
/// There is no <c>EmailVerified</c> flag. Descope verifies the address before the app is given the
/// session token that authorises these calls, so a record existing here already means the address was
/// verified — provided the token is validated, which is the API's job.
/// </remarks>
public class PendingRegistration
{
    /// <summary>Also becomes the created user's id, so the ID the app holds never changes.</summary>
    public Guid Id { get; set; }

    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// The Descope user id (`sub`) from the token that authorised this registration. Stored so the
    /// Auth API can map a Descope identity to a member later without going through the email address.
    /// </summary>
    public string? DescopeUserId { get; set; }

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public DateOnly DateOfBirth { get; set; }

    public string ZipCode { get; set; } = string.Empty;

    /// <summary>Optional — the only non-required field on the registration screen.</summary>
    public string? ContactNumber { get; set; }

    public DateTime CreatedUtc { get; set; }

    public DateTime ExpiresUtc { get; set; }

    public bool IsExpired(DateTimeOffset now) => now.UtcDateTime > ExpiresUtc;
}
