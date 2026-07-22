using System.Security.Cryptography;

namespace AuthApi.Infrastructure.Sso.OpenToken;

/// <summary>
/// Key derivation and cipher construction shared by <see cref="OpenTokenWriter"/> and
/// <see cref="OpenTokenReader"/>.
/// </summary>
internal static class OpenTokenKey
{
    private const int Pbkdf2Iterations = 1000;

    // The spec derives the key with a fixed all-zero salt; the shared secret itself is per connection.
    private static readonly byte[] KeyDerivationSalt = new byte[8];

    internal static byte[] Derive(byte[] sharedSecret, OpenTokenCipherSuite cipherSuite)
    {
        var keyLength = cipherSuite switch
        {
            OpenTokenCipherSuite.Null => 0,
            OpenTokenCipherSuite.Aes256Cbc => 32,
            OpenTokenCipherSuite.Aes128Cbc => 16,
            OpenTokenCipherSuite.TripleDes168Cbc => 24,
            _ => throw new ArgumentOutOfRangeException(nameof(cipherSuite), cipherSuite, "Unknown OpenToken cipher suite.")
        };

        return keyLength == 0
            ? []
            : Rfc2898DeriveBytes.Pbkdf2(sharedSecret, KeyDerivationSalt, Pbkdf2Iterations, HashAlgorithmName.SHA1, keyLength);
    }

    internal static SymmetricAlgorithm? CreateCipher(OpenTokenCipherSuite cipherSuite)
    {
        SymmetricAlgorithm? cipher = cipherSuite switch
        {
            OpenTokenCipherSuite.Null => null,
            OpenTokenCipherSuite.Aes256Cbc or OpenTokenCipherSuite.Aes128Cbc => Aes.Create(),
            // 3DES is part of the OpenToken spec and only used when a connection's agent file asks
            // for suite 3; prefer AES suites for new connections.
#pragma warning disable CA5350
            OpenTokenCipherSuite.TripleDes168Cbc => TripleDES.Create(),
#pragma warning restore CA5350
            _ => throw new ArgumentOutOfRangeException(nameof(cipherSuite), cipherSuite, "Unknown OpenToken cipher suite.")
        };

        if (cipher is not null)
        {
            cipher.Mode = CipherMode.CBC;
            cipher.Padding = PaddingMode.PKCS7;
        }

        return cipher;
    }
}
