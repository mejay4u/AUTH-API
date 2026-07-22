using System.Buffers.Binary;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;

namespace AuthApi.Infrastructure.Sso.OpenToken;

/// <summary>
/// Validates and decodes an OpenToken back into its attribute multi-map — the inverse of
/// <see cref="OpenTokenWriter"/> and the port of the agent SDK's <c>ReadTokenMultiStringDictionary</c>
/// used by the legacy inbound <c>ParseSSOTokenAsync</c> flow.
/// </summary>
public static class OpenTokenReader
{
    private const byte Version = 1;

    public static IReadOnlyDictionary<string, IReadOnlyList<string>> Read(string token, byte[] sharedSecret)
    {
        var raw = Convert.FromBase64String(token
            .Replace('-', '+')
            .Replace('_', '/')
            .Replace('*', '='));

        if (raw.Length < 27 || raw[0] != 'O' || raw[1] != 'T' || raw[2] != 'K')
        {
            throw new InvalidOperationException("Not an OpenToken: missing OTK header.");
        }

        if (raw[3] != Version)
        {
            throw new InvalidOperationException($"Unsupported OpenToken version {raw[3]}.");
        }

        var cipherSuite = (OpenTokenCipherSuite)raw[4];
        var mac = raw[5..25];
        var position = 25;

        var ivLength = raw[position++];
        var iv = raw[position..(position + ivLength)];
        position += ivLength;

        var keyInfoLength = raw[position++];
        position += keyInfoLength; // key info is not used for password-derived keys

        var payloadLength = BinaryPrimitives.ReadUInt16BigEndian(raw.AsSpan(position, 2));
        position += 2;
        var encrypted = raw[position..(position + payloadLength)];

        var key = OpenTokenKey.Derive(sharedSecret, cipherSuite);
        var compressed = Decrypt(encrypted, key, iv, cipherSuite);
        var payload = Decompress(compressed);

        if (!ComputeMac(key, cipherSuite, iv, payload).SequenceEqual(mac))
        {
            throw new InvalidOperationException("OpenToken MAC validation failed; the token is tampered or the shared secret is wrong.");
        }

        return ParsePayload(payload);
    }

    private static byte[] Decrypt(byte[] encrypted, byte[] key, byte[] iv, OpenTokenCipherSuite cipherSuite)
    {
        using var cipher = OpenTokenKey.CreateCipher(cipherSuite);
        if (cipher is null)
        {
            return encrypted;
        }

        cipher.Key = key;
        cipher.IV = iv;
        using var decryptor = cipher.CreateDecryptor();
        return decryptor.TransformFinalBlock(encrypted, 0, encrypted.Length);
    }

    private static byte[] ComputeMac(byte[] key, OpenTokenCipherSuite cipherSuite, byte[] iv, byte[] payload)
    {
        using var mac = key.Length > 0
            ? IncrementalHash.CreateHMAC(HashAlgorithmName.SHA1, key)
            : IncrementalHash.CreateHash(HashAlgorithmName.SHA1);

        mac.AppendData([Version]);
        mac.AppendData([(byte)cipherSuite]);
        mac.AppendData(iv);
        mac.AppendData(payload);
        return mac.GetHashAndReset();
    }

    private static byte[] Decompress(byte[] compressed)
    {
        using var input = new MemoryStream(compressed);
        using var zlib = new ZLibStream(input, CompressionMode.Decompress);
        using var output = new MemoryStream();
        zlib.CopyTo(output);
        return output.ToArray();
    }

    private static Dictionary<string, IReadOnlyList<string>> ParsePayload(byte[] payload)
    {
        var attributes = new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var line in Encoding.UTF8.GetString(payload).Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            var separator = line.IndexOf('=');
            if (separator <= 0)
            {
                continue;
            }

            var attributeKey = line[..separator];
            var value = line[(separator + 1)..];
            attributes[attributeKey] = attributes.TryGetValue(attributeKey, out var existing)
                ? [.. existing, value]
                : [value];
        }

        return attributes;
    }
}
