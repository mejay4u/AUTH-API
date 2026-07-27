using Registration.Domain.Common;

namespace Registration.Domain.Registration;

/// <summary>
/// Expected, business-rule errors returned by the registration/migration use cases. Each maps to an
/// HTTP status via <see cref="ErrorType"/>, keeping the handlers exception-free.
/// </summary>
public static class RegistrationErrors
{
    /// <summary>Legacy credentials could not be verified during JIT migration (generic — no enumeration).</summary>
    public static readonly Error LegacyVerificationFailed = Error.Unauthorized(
        "Registration.LegacyVerificationFailed",
        "The credentials could not be verified.");

    /// <summary>The Descope-pushed user payload was missing required fields.</summary>
    public static readonly Error InvalidUserPayload = Error.Validation(
        "Registration.InvalidUserPayload",
        "The user payload is missing required fields.");
}
