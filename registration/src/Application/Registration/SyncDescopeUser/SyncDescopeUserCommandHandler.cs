using MediatR;
using Microsoft.Extensions.Logging;
using Registration.Application.Common;
using Registration.Application.Common.Interfaces;
using Registration.Domain.Common;
using Registration.Domain.Users;

namespace Registration.Application.Registration.SyncDescopeUser;

/// <summary>
/// Idempotently upserts a Descope-pushed user into the profile store (matched by email). New records
/// get <see cref="UserOrigin.Registration"/>; existing records are updated in place. No passwords.
/// </summary>
public sealed class SyncDescopeUserCommandHandler(
    IUserProfileRepository repository,
    TimeProvider timeProvider,
    ILogger<SyncDescopeUserCommandHandler> logger)
    : IRequestHandler<SyncDescopeUserCommand, Result>
{
    public async Task<Result> Handle(SyncDescopeUserCommand request, CancellationToken cancellationToken)
    {
        var email = EmailNormalizer.Normalize(request.Email);
        var now = timeProvider.GetUtcNow().UtcDateTime;

        var user = await repository.GetByEmailAsync(email, cancellationToken)
                   ?? new User { Id = Guid.NewGuid(), Origin = UserOrigin.Registration, CreatedUtc = now };

        user.Email = email;
        user.Username = email;
        user.FirstName = request.FirstName.Trim();
        user.LastName = request.LastName.Trim();
        user.DateOfBirth = request.DateOfBirth;
        user.ZipCode = string.IsNullOrWhiteSpace(request.ZipCode) ? null : request.ZipCode.Trim();
        user.ContactNumber = string.IsNullOrWhiteSpace(request.ContactNumber) ? null : request.ContactNumber.Trim();
        user.DescopeUserId = request.DescopeUserId;
        user.UpdatedUtc = now;

        await repository.UpsertAsync(user, cancellationToken);

        logger.LogInformation("Synced Descope user {UserId} ({Origin}).", user.Id, user.Origin);
        return Result.Success();
    }
}
