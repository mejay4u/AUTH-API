using Registration.Application.Common.Models;

namespace Registration.Api.Contracts;

/// <summary>Request to send (or resend) an email verification code.</summary>
public sealed record SendOtpRequest(string Email);

/// <summary>Request to verify the emailed code.</summary>
public sealed record VerifyOtpRequest(string Email, string Code);

/// <summary>Create-account request. Email is the User ID / username.</summary>
public sealed record CreateAccountRequest(string Email, string Password, string ConfirmPassword);

/// <summary>Generic success message for the OTP endpoints.</summary>
public sealed record MessageResponse(string Message);

/// <summary>Response returned when an account is created.</summary>
public sealed record CreateAccountResponse(Guid UserId, string Email, string Username)
{
    public static CreateAccountResponse From(CreateAccountResult r) => new(r.UserId, r.Email, r.Username);
}
