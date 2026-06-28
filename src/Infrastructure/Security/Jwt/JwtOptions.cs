using System.ComponentModel.DataAnnotations;

namespace AuthApi.Infrastructure.Security.Jwt;

/// <summary>
/// JWT issuance/validation settings. Bound from configuration section "Jwt" and validated on startup
/// (fail-fast) so a misconfigured token pipeline can never start serving requests.
/// </summary>
public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    [Required]
    public string Issuer { get; init; } = string.Empty;

    [Required]
    public string Audience { get; init; } = string.Empty;

    [Range(1, 120)]
    public int AccessTokenMinutes { get; init; } = 15;

    [Range(1, 90)]
    public int RefreshTokenDays { get; init; } = 7;

    /// <summary>
    /// PEM-encoded RSA private key (PKCS#8). Preferred source in production is a secret store / Key
    /// Vault, injected here. If empty and <see cref="AllowEphemeralKey"/> is true, a key is generated
    /// and persisted under <see cref="KeyDirectory"/> for local development.
    /// </summary>
    public string? PrivateKeyPem { get; init; }

    /// <summary>Optional path to a PEM file holding the RSA private key.</summary>
    public string? PrivateKeyPath { get; init; }

    /// <summary>When true (development only), generate &amp; persist a signing key if none is provided.</summary>
    public bool AllowEphemeralKey { get; set; }

    /// <summary>Directory used to persist a generated development key so the JWKS stays stable across restarts.</summary>
    public string KeyDirectory { get; set; } = "keys";

    public TimeSpan AccessTokenLifetime => TimeSpan.FromMinutes(AccessTokenMinutes);
    public TimeSpan RefreshTokenLifetime => TimeSpan.FromDays(RefreshTokenDays);
}
