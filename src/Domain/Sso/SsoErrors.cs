using AuthApi.Domain.Common;

namespace AuthApi.Domain.Sso;

/// <summary>
/// Catalogue of SSO errors. The legacy controller signalled problems through a Status/MessageStatus
/// string pair on the response body; here they are typed errors that map to proper HTTP statuses.
/// </summary>
public static class SsoErrors
{
    public static readonly Error NotConfigured =
        Error.NotFound("Sso.NotConfigured", "No SSO configuration exists for the requested LOB and SSO name.");

    public static Error ProviderFailed(string ssoName) =>
        Error.Failure("Sso.ProviderFailed", $"The SSO provider for '{ssoName}' could not produce a sign-on URL.");
}
