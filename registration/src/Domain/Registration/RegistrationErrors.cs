using Registration.Domain.Common;

namespace Registration.Domain.Registration;

/// <summary>
/// The catalogue of expected, business-rule errors returned by the registration use cases.
/// Each maps to a specific HTTP status via <see cref="ErrorType"/>, keeping the handlers exception-free.
/// The descriptions surface to the member through the Descope flow, so they are written to be read by
/// one — and deliberately reveal nothing about Facets, tenants, or why a match failed.
/// </summary>
public static class RegistrationErrors
{
    // --- Pending registration record ---
    public static readonly Error RegistrationNotFoundOrExpired = Error.Validation(
        "Registration.NotFoundOrExpired",
        "The registration was not found or has expired. Please start again.");

    public static readonly Error EmailAlreadyRegistered = Error.Conflict(
        "Registration.EmailAlreadyRegistered",
        "An account with this email address already exists.");

    // --- Password ---
    public static readonly Error PasswordNotSet = Error.Conflict(
        "Registration.PasswordNotSet",
        "A password must be set before registration can be completed.");

    // --- Eligibility (Facets) ---
    public static readonly Error MemberNotFound = Error.Validation(
        "Registration.MemberNotFound",
        "We couldn't match those details to a member record. Check them and try again.");

    /// <summary>
    /// Deliberately identical in code and wording to <see cref="MemberNotFound"/>: telling the caller
    /// that a member *was* found but the details didn't line up would confirm the SSN belongs to
    /// someone. The distinction is kept in the logs, not in the response.
    /// </summary>
    public static readonly Error MemberDetailsMismatch = Error.Validation(
        "Registration.MemberNotFound",
        "We couldn't match those details to a member record. Check them and try again.");

    public static readonly Error EligibilityLookupFailed = Error.Failure(
        "Registration.EligibilityLookupFailed",
        "We couldn't verify your membership right now. Please try again shortly.");
}
