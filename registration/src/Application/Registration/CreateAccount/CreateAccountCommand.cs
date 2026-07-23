using MediatR;
using Registration.Application.Common.Models;
using Registration.Domain.Common;

namespace Registration.Application.Registration.CreateAccount;

/// <summary>
/// Create the portal user (Create Account screen). The email is the User ID / username. Requires the
/// email to have been verified via OTP first.
/// </summary>
public sealed record CreateAccountCommand(string Email, string Password, string ConfirmPassword)
    : IRequest<Result<CreateAccountResult>>;
