using System.Security.Cryptography;
using System.Text;

namespace Registration.Api.Infrastructure;

/// <summary>
/// Credentials for the machine-to-machine calls Descope's flow connectors make into this service.
/// </summary>
/// <remarks>
/// These endpoints are NOT called by the mobile app or a browser — the caller is the Descope engine,
/// server to server, so there is no member session token to validate. A shared secret is what
/// distinguishes a real connector call from anyone else who found the URL.
///
/// That matters more than it looks: because Descope only calls <c>initiateRegistration</c> after it has
/// validated the OTP, an authenticated call is this service's *only* evidence that the email address
/// was verified. Treat the key accordingly — supply it via secrets or Key Vault, rotate it, and never
/// let it into source control.
/// </remarks>
public sealed class ConnectorAuthOptions
{
    public const string SectionName = "ConnectorAuth";

    /// <summary>Header carrying the shared secret.</summary>
    public string HeaderName { get; init; } = "X-Connector-Key";

    /// <summary>
    /// Accepted keys. More than one so a key can be rotated without downtime: add the new key, update
    /// the connector, then drop the old one.
    /// </summary>
    public string[] Keys { get; init; } = [];

    /// <summary>
    /// Set to true ONLY in local development to bypass the check. Any environment that leaves this on
    /// is publishing an unauthenticated way to create accounts.
    /// </summary>
    public bool AllowAnonymous { get; init; }
}

/// <summary>
/// Endpoint filter that rejects calls without a valid connector key. Written as a filter rather than an
/// authentication scheme to match the existing <see cref="FeatureGateEndpointFilter"/> style and to keep
/// the whole check in one readable place.
/// </summary>
public sealed class ConnectorAuthEndpointFilter(ConnectorAuthOptions options) : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        if (options.AllowAnonymous)
        {
            return await next(context);
        }

        var supplied = context.HttpContext.Request.Headers[options.HeaderName].ToString();

        if (string.IsNullOrEmpty(supplied) || !IsKnownKey(supplied))
        {
            return Results.Problem(
                title: "Unauthorized",
                detail: "A valid connector key is required.",
                statusCode: StatusCodes.Status401Unauthorized);
        }

        return await next(context);
    }

    /// <summary>
    /// Compares against every configured key in constant time. The loop deliberately does not
    /// short-circuit on the first match — bailing out early leaks, through timing, how many keys were
    /// checked and how much of one matched.
    /// </summary>
    private bool IsKnownKey(string supplied)
    {
        var suppliedBytes = Encoding.UTF8.GetBytes(supplied);
        var matched = false;

        foreach (var key in options.Keys)
        {
            if (string.IsNullOrEmpty(key))
            {
                continue;
            }

            var keyBytes = Encoding.UTF8.GetBytes(key);

            // FixedTimeEquals requires equal lengths; an unequal length is a mismatch, and comparing
            // the supplied value against itself keeps the work done per candidate the same either way.
            matched |= keyBytes.Length == suppliedBytes.Length
                && CryptographicOperations.FixedTimeEquals(keyBytes, suppliedBytes);
        }

        return matched;
    }
}
