using MediatR;
using Registration.Application.Common.Models;
using Registration.Domain.Common;

namespace Registration.Application.Registration.CreateAccount;

/// <summary>
/// Final step: create the portal user from a verified registration session. The email (the User ID /
/// username) and personal information come from the session; the client supplies only the password.
/// </summary>
public sealed record CreateAccountCommand(
    Guid RegistrationId,
    string Password,
    string ConfirmPassword) : IRequest<Result<CreateAccountResult>>;
