using Registration.Application.Common.Models;

namespace Registration.Api.Contracts;

/// <summary>
/// Step 1: open a registration session with the personal information from the registration screen.
/// <c>DateOfBirth</c> is an ISO date (yyyy-MM-dd). <c>ContactNumber</c> is optional.
/// </summary>
public sealed record StartRegistrationRequest(
    string FirstName,
    string LastName,
    DateOnly DateOfBirth,
    string ZipCode,
    string Email,
    string? ContactNumber);

/// <summary>Response to <c>start</c> — the session id used by the remaining steps.</summary>
public sealed record StartRegistrationResponse(Guid RegistrationId)
{
    public static StartRegistrationResponse From(StartRegistrationResult r) => new(r.RegistrationId);
}

/// <summary>Resend the verification code for a session.</summary>
public sealed record ResendOtpRequest(Guid RegistrationId);

/// <summary>Verify the emailed code for a session.</summary>
public sealed record VerifyOtpRequest(Guid RegistrationId, string Code);

/// <summary>Create the account from a verified session — the client supplies only the password.</summary>
public sealed record CreateAccountRequest(Guid RegistrationId, string Password, string ConfirmPassword);

/// <summary>Generic success message for the OTP endpoints.</summary>
public sealed record MessageResponse(string Message);

/// <summary>Response returned when an account is created.</summary>
public sealed record CreateAccountResponse(Guid UserId, string Email, string Username)
{
    public static CreateAccountResponse From(CreateAccountResult r) => new(r.UserId, r.Email, r.Username);
}
