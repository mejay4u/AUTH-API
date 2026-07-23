using MediatR;
using Registration.Application.Common.Models;
using Registration.Domain.Common;

namespace Registration.Application.Registration.CreateAccount;

/// <summary>
/// Create the portal user (registration flow). The email is the User ID / username. Carries the
/// personal information collected on the registration screen plus the chosen password. Requires the
/// email to have been verified via OTP first.
/// </summary>
public sealed record CreateAccountCommand(
    string FirstName,
    string LastName,
    DateOnly DateOfBirth,
    string ZipCode,
    string Email,
    string? ContactNumber,
    string Password,
    string ConfirmPassword) : IRequest<Result<CreateAccountResult>>;
