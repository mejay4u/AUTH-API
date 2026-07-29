using System.Security.Claims;

namespace Registration.Api.Infrastructure;

/// <summary>
/// Settings for validating the Descope session tokens the app sends.
/// </summary>
/// <remarks>
/// The caller is the mobile app, carrying the session JWT Descope issued when it verified the
/// member's email. That token is this service's only evidence the address was verified, so it must be
/// validated properly — signature against Descope's JWKS, issuer, and expiry — not merely decoded.
/// </remarks>
public sealed class DescopeAuthOptions
{
    public const string SectionName = "Descope";

    /// <summary>Descope project ID. Also the token issuer.</summary>
    public string ProjectId { get; init; } = string.Empty;

    /// <summary>Descope's API base. Override only for a non-default region.</summary>
    public string BaseUrl { get; init; } = "https://api.descope.com";

    /// <summary>
    /// Development escape hatch: skip token validation entirely. Any environment that leaves this on
    /// is publishing an unauthenticated way to create accounts.
    /// </summary>
    public bool AllowAnonymous { get; init; }

    /// <summary>Where Descope publishes the signing keys for this project.</summary>
    public string JwksUri => $"{BaseUrl.TrimEnd('/')}/{ProjectId}/.well-known/jwks.json";

    /// <summary>OIDC discovery document, which is what JwtBearer prefers to read.</summary>
    public string MetadataAddress =>
        $"{BaseUrl.TrimEnd('/')}/{ProjectId}/.well-known/openid-configuration";
}

/// <summary>
/// Reads the verified email out of the validated token. Descope projects differ in which claim carries
/// it — and some don't include it at all by default — so several are tried before giving up.
/// </summary>
public static class DescopePrincipal
{
    public static string? GetEmail(ClaimsPrincipal principal)
    {
        foreach (var claimType in new[] { ClaimTypes.Email, "email", "loginId", "preferred_username" })
        {
            var value = principal.FindFirstValue(claimType);
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }

        return null;
    }

    /// <summary>The Descope user ID (`sub`) — worth storing alongside the member record.</summary>
    public static string? GetUserId(ClaimsPrincipal principal) =>
        principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? principal.FindFirstValue("sub");
}
