namespace Registration.Domain.Registration;

/// <summary>
/// The member record while registration is in progress — the "Pending" state of the sequence diagram.
/// Created by <c>initiateRegistration</c> once Descope has verified the email, gains a password hash at
/// the password step, and is promoted to a <see cref="Users.User"/> by <c>completeRegistration</c> after
/// eligibility is confirmed, at which point it is deleted.
/// </summary>
/// <remarks>
/// There is no <c>EmailVerified</c> flag any more. Descope owns email verification: it only calls
/// <c>initiateRegistration</c> after validating the OTP, so a record existing here already means the
/// address was verified. That guarantee is only as strong as the connector credential on the call —
/// see <c>ConnectorAuthOptions</c>.
/// </remarks>
public class PendingRegistration
{
    /// <summary>Also becomes the created user's id, so the ID Descope receives never changes.</summary>
    public Guid Id { get; set; }

    public string Email { get; set; } = string.Empty;

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public DateOnly DateOfBirth { get; set; }

    public string ZipCode { get; set; } = string.Empty;

    /// <summary>Null until the password step; never the plaintext password.</summary>
    public string? PasswordHash { get; set; }

    /// <summary>Null until the password step.</summary>
    public string? PasswordSalt { get; set; }

    public DateTime CreatedUtc { get; set; }

    public DateTime ExpiresUtc { get; set; }

    /// <summary>True once the password step has run — required before registration can complete.</summary>
    public bool HasPassword =>
        !string.IsNullOrEmpty(PasswordHash) && !string.IsNullOrEmpty(PasswordSalt);

    public bool IsExpired(DateTimeOffset now) => now.UtcDateTime > ExpiresUtc;
}
