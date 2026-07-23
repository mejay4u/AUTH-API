using System.Security.Cryptography;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using Microsoft.Extensions.Options;
using Registration.Application.Common.Interfaces;

namespace Registration.Infrastructure.Security.PasswordHashing;

/// <summary>
/// Best-practice password hasher using PBKDF2 (HMAC-SHA256) with a random per-password salt. Produces a
/// (hash, salt) pair — both Base64 — to fit the existing user table's two-column layout. Verification is
/// constant-time. All parameters come from <see cref="PasswordHashingOptions"/>, so the work factor can
/// be raised over time and the whole scheme swapped behind <see cref="IPasswordHasher"/>.
/// </summary>
public sealed class Pbkdf2PasswordHasher(IOptions<PasswordHashingOptions> options) : IPasswordHasher
{
    private readonly PasswordHashingOptions _options = options.Value;

    public (string Hash, string Salt) Hash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(_options.SaltSizeBytes);
        var hash = Derive(password, salt);
        return (Convert.ToBase64String(hash), Convert.ToBase64String(salt));
    }

    public bool Verify(string password, string storedHash, string storedSalt)
    {
        if (string.IsNullOrEmpty(storedHash) || string.IsNullOrEmpty(storedSalt))
        {
            return false;
        }

        byte[] expected;
        byte[] salt;
        try
        {
            expected = Convert.FromBase64String(storedHash);
            salt = Convert.FromBase64String(storedSalt);
        }
        catch (FormatException)
        {
            return false;
        }

        var actual = Derive(password, salt, expected.Length);
        return CryptographicOperations.FixedTimeEquals(actual, expected);
    }

    private byte[] Derive(string password, byte[] salt, int? outputBytes = null) =>
        KeyDerivation.Pbkdf2(
            password,
            salt,
            _options.Prf,
            _options.Iterations,
            outputBytes ?? _options.HashSizeBytes);
}
