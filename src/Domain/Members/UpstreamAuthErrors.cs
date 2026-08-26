using AuthApi.Domain.Common;

namespace AuthApi.Domain.Members;

/// <summary>
/// Errors raised by the BFA itself while relaying an authentication call to the upstream
/// ARTS auth service (via Apigee Internal).
///
/// These are deliberately few: in pass-through mode the BFA does NOT decide whether a credential is
/// valid — ARTS does. Anything ARTS answers (401, 403, 423, 200 …) is relayed verbatim and never
/// becomes an <see cref="Error"/>. These errors cover only the cases where the BFA could not obtain
/// an answer at all, plus the cheap shape check it performs before spending an upstream round trip.
/// </summary>
public static class UpstreamAuthErrors
{
    /// <summary>Apigee Internal / ARTS could not be reached (DNS, TLS, connection refused, 5xx from the proxy).</summary>
    public static readonly Error Unavailable = Error.Unavailable(
        "Auth.Upstream.Unavailable",
        "The authentication service is temporarily unavailable. Please try again.");

    /// <summary>The upstream call exceeded the configured timeout.</summary>
    public static readonly Error Timeout = Error.Timeout(
        "Auth.Upstream.Timeout",
        "The authentication service did not respond in time. Please try again.");

    /// <summary>The request body was not well-formed JSON, so there was nothing meaningful to forward.</summary>
    public static readonly Error MalformedPayload = Error.Validation(
        "Auth.Request.Malformed",
        "The request body must be a well-formed JSON object.");

    /// <summary>The request body exceeded the size the BFA is willing to buffer and relay.</summary>
    public static readonly Error PayloadTooLarge = Error.Validation(
        "Auth.Request.TooLarge",
        "The request body is larger than this endpoint accepts.");
}
