namespace Registration.Application.Common.Interfaces;

/// <summary>
/// Produces and verifies password hashes for new accounts. Kept as an abstraction so the hashing
/// scheme (PBKDF2 today, Argon2id/BCrypt tomorrow) can be swapped in Infrastructure via configuration
/// without touching any use-case logic. Returns a (hash, salt) pair to fit the existing user table's
/// two-column layout.
/// </summary>
public interface IPasswordHasher
{
    /// <summary>Produce a new (hash, salt) pair for the given plaintext password.</summary>
    (string Hash, string Salt) Hash(string password);

    /// <summary>Constant-time verification of a password against a stored hash + salt.</summary>
    bool Verify(string password, string storedHash, string storedSalt);
}
