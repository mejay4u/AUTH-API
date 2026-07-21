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
}
