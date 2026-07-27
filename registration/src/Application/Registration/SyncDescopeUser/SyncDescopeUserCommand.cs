using MediatR;
using Registration.Domain.Common;

namespace Registration.Application.Registration.SyncDescopeUser;

/// <summary>
/// Upsert a user that Descope pushed to us (on registration or profile update). Idempotent — safe to
/// replay on webhook retries. Email is the identifier/username.
/// </summary>
public sealed record SyncDescopeUserCommand(
    string DescopeUserId,
    string Email,
    string FirstName,
    string LastName,
    DateOnly? DateOfBirth,
    string? ZipCode,
    string? ContactNumber) : IRequest<Result>;
