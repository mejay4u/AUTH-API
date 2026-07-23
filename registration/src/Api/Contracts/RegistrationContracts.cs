using Registration.Application.Common.Models;

namespace Registration.Api.Contracts;

/// <summary>Request to send (or resend) an email verification code.</summary>
public sealed record SendOtpRequest(string Email);

/// <summary>Request to verify the emailed code.</summary>
public sealed record VerifyOtpRequest(string Email, string Code);

/// <summary>
/// Create-account request. Email is the User ID / username. Carries the personal information from the
/// registration screen. <c>DateOfBirth</c> is an ISO date (yyyy-MM-dd). <c>ContactNumber</c> is optional.
/// </summary>
public sealed record CreateAccountRequest(
    string FirstName,
    string LastName,
    DateOnly DateOfBirth,
    string ZipCode,
    string Email,
    string? ContactNumber,
    string Password,
    string ConfirmPassword);

/// <summary>Generic success message for the OTP endpoints.</summary>
public sealed record MessageResponse(string Message);

/// <summary>Response returned when an account is created.</summary>
public sealed record CreateAccountResponse(Guid UserId, string Email, string Username)
{
    public static CreateAccountResponse From(CreateAccountResult r) => new(r.UserId, r.Email, r.Username);
}
