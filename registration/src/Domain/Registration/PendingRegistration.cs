namespace Registration.Domain.Registration;

/// <summary>
/// A server-side registration session for the pre-account steps of the onboarding wizard. Holds the
/// personal information collected on Step 1 and whether the email has been verified, until the account
/// is created (Step 4) — at which point it is promoted to a <c>User</c> and deleted. Lives in the
/// registration database so the server, not the client, is the source of truth for the in-progress
/// registration.
/// </summary>
public class PendingRegistration
{
    public Guid Id { get; set; }

    public string Email { get; set; } = string.Empty;

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public DateOnly DateOfBirth { get; set; }

    public string ZipCode { get; set; } = string.Empty;

    public string? ContactNumber { get; set; }

    public bool EmailVerified { get; set; }

    public DateTime CreatedUtc { get; set; }

    public DateTime ExpiresUtc { get; set; }

    public bool IsExpired(DateTimeOffset now) => now.UtcDateTime > ExpiresUtc;
}
