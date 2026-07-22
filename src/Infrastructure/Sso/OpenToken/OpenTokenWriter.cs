using System.Buffers.Binary;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;

namespace AuthApi.Infrastructure.Sso.OpenToken;

/// <summary>
/// Cipher suites defined by the OpenToken specification (draft-smith-opentoken). The numeric values
/// are the ids used in the token header and in the agent configuration's <c>cipher-suite</c> property.
/// </summary>
public enum OpenTokenCipherSuite
{
    Null = 0,
    Aes256Cbc = 1,
    Aes128Cbc = 2,
    TripleDes168Cbc = 3
}

/// <summary>
/// Generates PingFederate OpenTokens — the port of what the legacy code delegated to the PingFederate
/// agent SDK's <c>Agent.WriteTokenAsync</c>. A token is the member's attribute set ("user info"),
/// deflate-compressed, encrypted with a key derived from the agent's shared secret, wrapped in the
/// OTK binary envelope, and encoded cookie/URL-safe (base64 with <c>+/=</c> → <c>-_*</c>) so it can be
/// appended to the sign-on URL as a query parameter (e.g. <c>JivaZeomegaOpenToken=...</c>).
/// </summary>
public static class OpenTokenWriter
{
    private const byte Version = 1;

    public static string Write(
        IEnumerable<KeyValuePair<string, string>> attributes,
        byte[] sharedSecret,
        OpenTokenCipherSuite cipherSuite)
    {
        var payload = SerializePayload(attributes);
        var key = OpenTokenKey.Derive(sharedSecret, cipherSuite);

        byte[] iv;
        byte[] encrypted;
        using (var cipher = OpenTokenKey.CreateCipher(cipherSuite))
        {
            if (cipher is null)
            {
                iv = [];
                encrypted = Compress(payload);
            }
            else
            {
                cipher.Key = key;
                cipher.GenerateIV();
                iv = cipher.IV;
                using var encryptor = cipher.CreateEncryptor();
                var compressed = Compress(payload);
                encrypted = encryptor.TransformFinalBlock(compressed, 0, compressed.Length);
            }
        }

        // The MAC covers the header fields and the *cleartext* payload, keyed with the cipher key.
        var mac = ComputeMac(key, cipherSuite, iv, payload);

        using var token = new MemoryStream();
        token.Write("OTK"u8);
        token.WriteByte(Version);
        token.WriteByte((byte)cipherSuite);
        token.Write(mac);
        token.WriteByte((byte)iv.Length);
        token.Write(iv);
        token.WriteByte(0); // no key info block

        Span<byte> length = stackalloc byte[2];
        BinaryPrimitives.WriteUInt16BigEndian(length, checked((ushort)encrypted.Length));
        token.Write(length);
        token.Write(encrypted);

        return Convert.ToBase64String(token.ToArray())
            .Replace('+', '-')
            .Replace('/', '_')
            .Replace('=', '*');
    }

    /// <summary>
    /// One <c>key=value</c> line per attribute — the wire format the OpenToken adapter parses back
    /// into its <c>MultiStringDictionary</c>. Values must not contain line breaks.
    /// </summary>
    private static byte[] SerializePayload(IEnumerable<KeyValuePair<string, string>> attributes)
    {
        var builder = new StringBuilder();
        foreach (var (attributeKey, value) in attributes)
        {
            if (value.Contains('\n') || value.Contains('\r'))
            {
                throw new InvalidOperationException(
                    $"OpenToken attribute '{attributeKey}' contains a line break, which the payload format cannot represent.");
            }

            builder.Append(attributeKey).Append('=').Append(value).Append('\n');
        }

        return Encoding.UTF8.GetBytes(builder.ToString());
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

    private static byte[] Compress(byte[] payload)
    {
        using var buffer = new MemoryStream();
        using (var zlib = new ZLibStream(buffer, CompressionLevel.Optimal, leaveOpen: true))
        {
            zlib.Write(payload);
        }

        return buffer.ToArray();
    }
}
