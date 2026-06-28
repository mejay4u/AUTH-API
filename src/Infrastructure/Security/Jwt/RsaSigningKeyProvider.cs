using System.Security.Cryptography;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace AuthApi.Infrastructure.Security.Jwt;

/// <summary>
/// Owns the RSA key used to sign access tokens with RS256 (asymmetric).
///
/// Why asymmetric? In a microservices setup the Auth API signs with the PRIVATE key, while every other
/// service validates with the PUBLIC key (served from the JWKS endpoint). No downstream service ever
/// holds a secret capable of minting tokens — far safer than a shared HMAC secret.
///
/// Registered as a singleton so the key (and its <c>kid</c>) are stable for the process lifetime.
/// </summary>
public sealed class RsaSigningKeyProvider : IDisposable
{
    private readonly RSA _rsa;

    public RsaSigningKeyProvider(IOptions<JwtOptions> options)
    {
        var opt = options.Value;
        _rsa = LoadOrCreateKey(opt);

        KeyId = ComputeKid(_rsa);

        SigningKey = new RsaSecurityKey(_rsa) { KeyId = KeyId };
        SigningCredentials = new SigningCredentials(SigningKey, SecurityAlgorithms.RsaSha256);

        // Public-only key for token validation and JWKS publication.
        var publicRsa = RSA.Create();
        publicRsa.ImportParameters(_rsa.ExportParameters(includePrivateParameters: false));
        PublicKey = new RsaSecurityKey(publicRsa) { KeyId = KeyId };
    }

    public string KeyId { get; }
    public RsaSecurityKey SigningKey { get; }
    public RsaSecurityKey PublicKey { get; }
    public SigningCredentials SigningCredentials { get; }

    /// <summary>Public JWKS (JSON Web Key Set) for downstream services to validate tokens.</summary>
    public JsonWebKeySet GetJsonWebKeySet()
    {
        var jwk = JsonWebKeyConverter.ConvertFromRSASecurityKey(PublicKey);
        jwk.Use = "sig";
        jwk.Alg = SecurityAlgorithms.RsaSha256;
        jwk.KeyId = KeyId;

        var set = new JsonWebKeySet();
        set.Keys.Add(jwk);
        return set;
    }

    private static RSA LoadOrCreateKey(JwtOptions opt)
    {
        if (!string.IsNullOrWhiteSpace(opt.PrivateKeyPem))
        {
            var rsa = RSA.Create();
            rsa.ImportFromPem(opt.PrivateKeyPem);
            return rsa;
        }

        if (!string.IsNullOrWhiteSpace(opt.PrivateKeyPath) && File.Exists(opt.PrivateKeyPath))
        {
            var rsa = RSA.Create();
            rsa.ImportFromPem(File.ReadAllText(opt.PrivateKeyPath));
            return rsa;
        }

        if (!opt.AllowEphemeralKey)
        {
            throw new InvalidOperationException(
                "No JWT signing key configured. Set Jwt:PrivateKeyPem or Jwt:PrivateKeyPath. " +
                "Ephemeral key generation is only permitted in Development.");
        }

        // Development convenience: generate a 3072-bit key and persist it so the JWKS is stable.
        Directory.CreateDirectory(opt.KeyDirectory);
        var keyFile = Path.Combine(opt.KeyDirectory, "jwt-signing.pem");

        var generated = RSA.Create(3072);
        if (File.Exists(keyFile))
        {
            generated.ImportFromPem(File.ReadAllText(keyFile));
        }
        else
        {
            File.WriteAllText(keyFile, generated.ExportPkcs8PrivateKeyPem());
        }

        return generated;
    }

    private static string ComputeKid(RSA rsa)
    {
        var p = rsa.ExportParameters(includePrivateParameters: false);
        var material = new byte[p.Modulus!.Length + p.Exponent!.Length];
        Buffer.BlockCopy(p.Modulus, 0, material, 0, p.Modulus.Length);
        Buffer.BlockCopy(p.Exponent, 0, material, p.Modulus.Length, p.Exponent.Length);
        return Base64UrlEncoder.Encode(SHA256.HashData(material));
    }

    public void Dispose() => _rsa.Dispose();
}
