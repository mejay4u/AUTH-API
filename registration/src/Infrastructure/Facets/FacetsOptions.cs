namespace Registration.Infrastructure.Facets;

/// <summary>
/// Settings for the Facets eligibility lookup (bound from the "Facets" section).
/// </summary>
public sealed class FacetsOptions
{
    public const string SectionName = "Facets";

    /// <summary>
    /// Which implementation to use. "Http" talks to the real Facets service; "Stub" returns a
    /// synthetic match so the flow can be walked end-to-end before that integration exists.
    /// </summary>
    public string Provider { get; init; } = "Http";

    /// <summary>Base address of the Facets-facing service. Required when Provider = Http.</summary>
    public string BaseUrl { get; init; } = string.Empty;

    /// <summary>Path of the member-search endpoint, relative to <see cref="BaseUrl"/>.</summary>
    public string MemberSearchPath { get; init; } = "/members/search";

    /// <summary>API key or token for the Facets-facing service. Supply via secrets, never source.</summary>
    public string ApiKey { get; init; } = string.Empty;

    /// <summary>How long to wait before giving up on a lookup.</summary>
    public int TimeoutSeconds { get; init; } = 15;

    public bool UseStub => Provider.Equals("Stub", StringComparison.OrdinalIgnoreCase);
}
