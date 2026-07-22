namespace AuthApi.Application.Sso;

/// <summary>
/// Boundary to PingFederate. The surface mirrors the legacy <c>PingFedService</c> one-to-one so each
/// existing implementation can be ported behind this interface without reshaping its callers.
/// </summary>
public interface IPingFederateService
{
    Task<string?> GetAbarcaSsoAsync(SsoUrlContext context, CancellationToken cancellationToken);

    Task<string?> GetHraJivaSsoAsync(SsoUrlContext context, CancellationToken cancellationToken);

    Task<string?> GetPlanOfCareSsoAsync(SsoUrlContext context, CancellationToken cancellationToken);

    Task<string?> GetChatSsoAsync(SsoUrlContext context, CancellationToken cancellationToken);

    Task<string?> GetSoftheonSsoAsync(SsoUrlContext context, CancellationToken cancellationToken);

    Task<string?> GetCertifiSsoAsync(
        SsoUrlContext context,
        SsoAudience audience,
        IReadOnlyDictionary<string, string> attributes,
        CancellationToken cancellationToken);

    Task<string?> GetSdsSsoAsync(
        SsoUrlContext context,
        SsoAudience audience,
        IReadOnlyDictionary<string, string> attributes,
        CancellationToken cancellationToken);

    /// <summary>
    /// Generates a bare OpenToken (no URL) with the given agent configuration — the legacy
    /// <c>GenerateSSOToken</c> (BCBSMI hand-off, which stripped the URL off again after writing).
    /// </summary>
    Task<string?> GenerateSsoTokenAsync(
        string agentFileName,
        IReadOnlyDictionary<string, string> attributes,
        CancellationToken cancellationToken);

    /// <summary>
    /// Validates and decodes an inbound OpenToken into its attribute multi-map — the legacy
    /// <c>ParseSSOTokenAsync</c>. Returns null when the token fails validation. The legacy
    /// <c>ParseSSOTokenCSR</c> variant is deliberately not ported.
    /// </summary>
    Task<IReadOnlyDictionary<string, IReadOnlyList<string>>?> ParseSsoTokenAsync(
        string agentFileName,
        string token,
        CancellationToken cancellationToken);
}
