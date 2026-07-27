namespace Registration.Api.Infrastructure;

/// <summary>
/// Configuration for verifying inbound Descope calls (the user-sync webhook and the JIT verify hook).
/// Secrets come from Key Vault / user-secrets, never source. The exact header names + signed-payload
/// format must match your Descope configuration.
/// </summary>
public sealed class DescopeOptions
{
    public const string SectionName = "Descope";

    /// <summary>Shared HMAC signing secret used to validate the request signature.</summary>
    public string SigningSecret { get; init; } = string.Empty;

    /// <summary>Allowed clock skew (seconds) between the signed timestamp and now (replay protection).</summary>
    public int TimestampToleranceSeconds { get; init; } = 300;

    public string SignatureHeader { get; init; } = "x-descope-signature";

    public string TimestampHeader { get; init; } = "x-descope-timestamp";

    /// <summary>When false (e.g. Development), signature verification is skipped. Must be true in production.</summary>
    public bool Enabled { get; init; } = true;
}
