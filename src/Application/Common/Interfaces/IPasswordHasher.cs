namespace AuthApi.Application.Common.Interfaces;

/// <summary>
/// Verifies a plaintext password against the salted hash stored in the existing member database.
/// Kept as an abstraction so the exact legacy scheme (algorithm / salt order / encoding) can be
/// configured in Infrastructure without touching use-case logic.
/// </summary>
public interface IPasswordHasher
{
    /// <summary>Constant-time verification of a password against a stored hash + salt.</summary>
    bool Verify(string password, string storedHash, string storedSalt);

    /// <summary>Produce a new (hash, salt) pair — used for seeding mock data and tests.</summary>
    (string Hash, string Salt) Hash(string password);
}
