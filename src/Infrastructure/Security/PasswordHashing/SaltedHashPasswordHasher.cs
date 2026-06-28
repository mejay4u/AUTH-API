using System.Security.Cryptography;
using System.Text;
using AuthApi.Application.Common.Interfaces;
using Microsoft.Extensions.Options;

namespace AuthApi.Infrastructure.Security.PasswordHashing;

/// <summary>
/// Verifies passwords against a stored salted SHA-256/512 hash, matching the legacy scheme described
/// by <see cref="PasswordHashingOptions"/>. Comparison is constant-time to resist timing attacks.
///
/// IMPORTANT: when you connect the real database, set the options to match exactly how passwords were
/// originally hashed (algorithm, salt placement, encoding, iterations). This class never deals in
/// plaintext beyond the single verification call.
/// </summary>
public sealed class SaltedHashPasswordHasher(IOptions<PasswordHashingOptions> options) : IPasswordHasher
{
    private readonly PasswordHashingOptions _options = options.Value;

    public bool Verify(string password, string storedHash, string storedSalt)
    {
        if (string.IsNullOrEmpty(storedHash) || string.IsNullOrEmpty(storedSalt))
        {
            return false;
        }

        byte[] expected;
        byte[] saltBytes;
        try
        {
            expected = Decode(storedHash, _options.HashEncoding);
            saltBytes = Decode(storedSalt, _options.SaltEncoding);
        }
        catch (FormatException)
        {
            return false;
        }

        var actual = ComputeHash(password, saltBytes);
        return CryptographicOperations.FixedTimeEquals(actual, expected);
    }

    public (string Hash, string Salt) Hash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(_options.SaltSizeBytes);
        var hash = ComputeHash(password, salt);
        return (Encode(hash, _options.HashEncoding), Encode(salt, _options.SaltEncoding));
    }

    private byte[] ComputeHash(string password, byte[] salt)
    {
        var passwordBytes = Encoding.UTF8.GetBytes(password);

        var input = _options.SaltPlacement == SaltPlacement.Prefix
            ? Concat(salt, passwordBytes)
            : Concat(passwordBytes, salt);

        var result = HashOnce(input);
        for (var i = 1; i < _options.Iterations; i++)
        {
            result = HashOnce(result);
        }

        return result;
    }

    private byte[] HashOnce(byte[] input) => _options.Algorithm switch
    {
        HashAlgorithmKind.Sha256 => SHA256.HashData(input),
        HashAlgorithmKind.Sha512 => SHA512.HashData(input),
        _ => throw new InvalidOperationException($"Unsupported algorithm '{_options.Algorithm}'.")
    };

    private static byte[] Concat(byte[] a, byte[] b)
    {
        var result = new byte[a.Length + b.Length];
        Buffer.BlockCopy(a, 0, result, 0, a.Length);
        Buffer.BlockCopy(b, 0, result, a.Length, b.Length);
        return result;
    }

    private static byte[] Decode(string value, BinaryEncoding encoding) => encoding switch
    {
        BinaryEncoding.Base64 => Convert.FromBase64String(value),
        BinaryEncoding.Hex => Convert.FromHexString(value),
        _ => throw new InvalidOperationException($"Unsupported encoding '{encoding}'.")
    };

    private static string Encode(byte[] value, BinaryEncoding encoding) => encoding switch
    {
        BinaryEncoding.Base64 => Convert.ToBase64String(value),
        BinaryEncoding.Hex => Convert.ToHexString(value),
        _ => throw new InvalidOperationException($"Unsupported encoding '{encoding}'.")
    };
}
