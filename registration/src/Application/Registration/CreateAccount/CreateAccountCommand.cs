using MediatR;
using Registration.Application.Common.Models;
using Registration.Domain.Common;

namespace Registration.Application.Registration.CreateAccount;

/// <summary>
/// Final step: create the portal account from a pending registration. The email
/// (the User ID / username) and personal information come from the pending
/// record; the client supplies only the password.
/// </summary>
public sealed record CreateAccountCommand(
    Guid UserId,
    string Password,
    string ConfirmPassword) : IRequest<Result<CreateAccountResult>>;
