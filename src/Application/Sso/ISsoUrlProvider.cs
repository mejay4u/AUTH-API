namespace AuthApi.Application.Sso;

/// <summary>
/// Strategy that produces the federated sign-on URL for one SSO integration. Replaces the legacy
/// <c>switch (SSOName)</c> block in <c>MemberService.GetSSOPerLOB</c>: adding an integration is a new
/// implementation plus a DI registration, not another case in a 100-line method.
/// </summary>
public interface ISsoUrlProvider
{
    /// <summary>The SSO name this provider handles (matched case-insensitively).</summary>
    string SsoName { get; }

    /// <summary>Returns the sign-on URL, or null/empty when the integration produces none.</summary>
    Task<string?> GetSsoUrlAsync(SsoUrlContext context, CancellationToken cancellationToken);
}
