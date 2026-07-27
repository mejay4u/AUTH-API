namespace Registration.Infrastructure.Legacy;

/// <summary>Configuration for the legacy member verification API used during JIT migration.</summary>
public sealed class LegacyVerifyApiOptions
{
    public const string SectionName = "LegacyVerifyApi";

    /// <summary>Base URL of the legacy verify API.</summary>
    public string BaseUrl { get; init; } = string.Empty;

    /// <summary>Optional API key sent as <c>X-Api-Key</c>.</summary>
    public string? ApiKey { get; init; }

    /// <summary>When true, use the in-process mock verifier (Development) instead of calling the API.</summary>
    public bool UseMock { get; init; }
}
