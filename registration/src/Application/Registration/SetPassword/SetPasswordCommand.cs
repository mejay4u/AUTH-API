using MediatR;
using Registration.Domain.Common;

namespace Registration.Application.Registration.SetPassword;

/// <summary>
/// Phase 3 of the flow: the member chose a password. It is stored here, hashed — Descope never holds
/// it, which is why sign-in has to be validated against this database.
/// </summary>
public sealed record SetPasswordCommand(
    Guid UserId,
    string Password,
    string ConfirmPassword) : IRequest<Result>;
