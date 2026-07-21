using AuthApi.Application.Sso;
using Microsoft.Extensions.Logging;

namespace AuthApi.Infrastructure.Sso;

/// <summary>
/// Placeholder PingFederate boundary so the API runs end-to-end before the real integration is wired.
/// Every method returns the configured PingFed URL for the SSO and logs that it is stubbed.
/// TODO: replace with an adapter that ports the legacy <c>PingFedService</c> implementations
/// (Abarca/HRA Jiva/Plan of Care/Chat/Softheon/Certifi/SDS) behind <see cref="IPingFederateService"/>.
/// </summary>
public sealed class StubPingFederateService(ILogger<StubPingFederateService> logger) : IPingFederateService
{
    public Task<string?> GetAbarcaSsoAsync(SsoUrlContext context, CancellationToken cancellationToken) =>
        FromConfiguredUrl(context);

    public Task<string?> GetHraJivaSsoAsync(SsoUrlContext context, CancellationToken cancellationToken) =>
        FromConfiguredUrl(context);

    public Task<string?> GetPlanOfCareSsoAsync(SsoUrlContext context, CancellationToken cancellationToken) =>
        FromConfiguredUrl(context);

    public Task<string?> GetChatSsoAsync(SsoUrlContext context, CancellationToken cancellationToken) =>
        FromConfiguredUrl(context);

    public Task<string?> GetSoftheonSsoAsync(SsoUrlContext context, CancellationToken cancellationToken) =>
        FromConfiguredUrl(context);

    public Task<string?> GetCertifiSsoAsync(
        SsoUrlContext context,
        SsoAudience audience,
        IReadOnlyDictionary<string, string> attributes,
        CancellationToken cancellationToken)
    {
        // Attribute keys only — the values carry member PII and must not be logged.
        logger.LogInformation(
            "Stubbed CERTIFI SSO for audience {Audience} with attributes [{Keys}].",
            audience, string.Join(", ", attributes.Keys));
        return FromConfiguredUrl(context);
    }

    public Task<string?> GetSdsSsoAsync(
        SsoUrlContext context,
        SsoAudience audience,
        IReadOnlyDictionary<string, string> attributes,
        CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Stubbed SDS SSO for audience {Audience} with attributes [{Keys}].",
            audience, string.Join(", ", attributes.Keys));
        return FromConfiguredUrl(context);
    }

    private Task<string?> FromConfiguredUrl(SsoUrlContext context)
    {
        logger.LogWarning(
            "PingFederate integration is stubbed; returning the configured URL for {SsoName}.",
            context.SsoName);
        return Task.FromResult(context.Configurations.Count > 0 ? context.Configurations[0].PingFedUrl : null);
    }
}
