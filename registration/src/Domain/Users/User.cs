namespace Registration.Domain.Users;

/// <summary>
/// A registered portal user. Lives in the registration service's OWN database. The email address is
/// the username / User ID. Passwords are stored only as a salted hash — never in plaintext.
/// </summary>
public class User
{
    public Guid Id { get; set; }

    public string Email { get; set; } = string.Empty;

    /// <summary>Login identifier — equal to <see cref="Email"/>.</summary>
    public string Username { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public string PasswordSalt { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedUtc { get; set; }
}
