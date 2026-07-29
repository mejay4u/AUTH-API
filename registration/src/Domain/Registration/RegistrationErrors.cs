using Registration.Domain.Common;

namespace Registration.Domain.Registration;

/// <summary>
/// The catalogue of expected, business-rule errors returned by the registration use cases.
/// Each maps to a specific HTTP status via <see cref="ErrorType"/>, keeping the handlers exception-free.
/// The descriptions are shown to the member by the app, so they are written to be read by one.
/// </summary>
public static class RegistrationErrors
{
    public static readonly Error RegistrationNotFoundOrExpired = Error.Validation(
        "Registration.NotFoundOrExpired",
        "The registration was not found or has expired. Please start again.");

    public static readonly Error EmailAlreadyRegistered = Error.Conflict(
        "Registration.EmailAlreadyRegistered",
        "An account with this email address already exists.");

    /// <summary>The bearer token's email doesn't match the registration being acted on.</summary>
    public static readonly Error EmailMismatch = Error.Forbidden(
        "Registration.EmailMismatch",
        "This registration belongs to a different email address.");
}
