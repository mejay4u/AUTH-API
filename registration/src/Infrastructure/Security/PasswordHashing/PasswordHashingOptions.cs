using Microsoft.AspNetCore.Cryptography.KeyDerivation;

namespace Registration.Infrastructure.Security.PasswordHashing;

/// <summary>
/// Configurable password-hashing parameters (bound from "PasswordHashing"). Defaults to a modern,
/// best-practice PBKDF2 (HMAC-SHA256) configuration. <see cref="Scheme"/> is the seam for swapping in
/// a different KDF (e.g. Argon2id/BCrypt) behind <c>IPasswordHasher</c> without changing use cases.
/// </summary>
public sealed class PasswordHashingOptions
{
    public const string SectionName = "PasswordHashing";

    /// <summary>Selected hashing scheme. Only "Pbkdf2" is implemented today; kept for future swaps.</summary>
    public string Scheme { get; init; } = "Pbkdf2";

    public KeyDerivationPrf Prf { get; init; } = KeyDerivationPrf.HMACSHA256;

    /// <summary>PBKDF2 iteration count. Tune upward as hardware improves.</summary>
    public int Iterations { get; init; } = 210_000;

    /// <summary>Size of the randomly generated per-password salt, in bytes.</summary>
    public int SaltSizeBytes { get; init; } = 16;

    /// <summary>Size of the derived key (hash), in bytes.</summary>
    public int HashSizeBytes { get; init; } = 32;
}
