using Registration.Domain.Common;

namespace Registration.Domain.Registration;

/// <summary>
/// The catalogue of expected, business-rule errors returned by the registration use cases.
/// Each maps to a specific HTTP status via <see cref="ErrorType"/>, keeping the handlers exception-free.
/// </summary>
public static class RegistrationErrors
{
    // --- Account creation ---
    public static readonly Error EmailAlreadyRegistered = Error.Conflict(
        "Registration.EmailAlreadyRegistered",
        "An account with this email address already exists.");

    public static readonly Error EmailNotVerified = Error.Conflict(
        "Registration.EmailNotVerified",
        "The email address must be verified before an account can be created.");

    // --- OTP ---
    public static readonly Error OtpInvalid = Error.Validation(
        "Registration.OtpInvalid",
        "The verification code is incorrect.");

    public static readonly Error OtpExpiredOrNotFound = Error.Validation(
        "Registration.OtpExpiredOrNotFound",
        "The verification code has expired or was never issued. Request a new code.");

    public static readonly Error OtpTooManyAttempts = Error.TooManyRequests(
        "Registration.OtpTooManyAttempts",
        "Too many incorrect attempts for this code. Request a new code.");

    public static readonly Error OtpResendTooSoon = Error.TooManyRequests(
        "Registration.OtpResendTooSoon",
        "A verification code was requested recently. Please wait before requesting another.");

    public static readonly Error OtpRequestLimitReached = Error.TooManyRequests(
        "Registration.OtpRequestLimitReached",
        "The maximum number of verification codes for this email has been reached.");
}
