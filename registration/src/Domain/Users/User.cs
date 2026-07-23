namespace Registration.Domain.Users;

/// <summary>
/// A registered portal user. Lives in the registration service's OWN database. The email address is
/// the username / User ID. Passwords are stored only as a salted hash — never in plaintext.
/// Captures the personal information collected on the registration screen.
/// </summary>
public class User
{
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

    /// <summary>Optional — the only non-required field on the screen.</summary>
    public string? ContactNumber { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedUtc { get; set; }
}
