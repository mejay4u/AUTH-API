namespace AuthApi.Infrastructure.Security.PasswordHashing;

public enum HashAlgorithmKind
{
    Sha256,
    Sha512
}

public enum SaltPlacement
{
    /// <summary>hash = H(salt || password)</summary>
    Prefix,

    /// <summary>hash = H(password || salt)</summary>
    Suffix
}

public enum BinaryEncoding
{
    Base64,
    Hex
}

/// <summary>
/// Describes how passwords are hashed in the existing database so verification can be matched exactly
/// without code changes. Bound from configuration section "PasswordHashing".
/// </summary>
public sealed class PasswordHashingOptions
{
    public const string SectionName = "PasswordHashing";

    public HashAlgorithmKind Algorithm { get; init; } = HashAlgorithmKind.Sha512;
    public SaltPlacement SaltPlacement { get; init; } = SaltPlacement.Prefix;

    /// <summary>How the stored hash string is encoded.</summary>
    public BinaryEncoding HashEncoding { get; init; } = BinaryEncoding.Base64;

    /// <summary>How the stored salt string is encoded.</summary>
    public BinaryEncoding SaltEncoding { get; init; } = BinaryEncoding.Base64;

    /// <summary>Number of times the hash function is applied (1 = single pass). Set to match legacy schemes.</summary>
    public int Iterations { get; init; } = 1;

    /// <summary>Salt size in bytes when generating new hashes (seeding/tests).</summary>
    public int SaltSizeBytes { get; init; } = 16;
}
