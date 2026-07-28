using MediatR;
using Microsoft.Extensions.Logging;
using Registration.Application.Common.Interfaces;
using Registration.Domain.Common;
using Registration.Domain.Registration;

namespace Registration.Application.Registration.SetPassword;

/// <summary>
/// Hashes and stores the password against the Pending record. Re-running it simply replaces the hash,
/// so a member who backs up a screen in the flow and picks a different password is fine.
/// </summary>
public sealed class SetPasswordCommandHandler(
    IPendingRegistrationRepository pendingRepository,
    IPasswordHasher passwordHasher,
    TimeProvider timeProvider,
    ILogger<SetPasswordCommandHandler> logger)
    : IRequestHandler<SetPasswordCommand, Result>
{
    public async Task<Result> Handle(SetPasswordCommand request, CancellationToken cancellationToken)
    {
        var pending = await pendingRepository.GetAsync(request.UserId, cancellationToken);
        if (pending is null || pending.IsExpired(timeProvider.GetUtcNow()))
        {
            // Result (non-generic) has no implicit conversion from Error — only Result<T> does.
            return Result.Failure(RegistrationErrors.RegistrationNotFoundOrExpired);
        }

        var (hash, salt) = passwordHasher.Hash(request.Password);
        await pendingRepository.SetPasswordAsync(pending.Id, hash, salt, cancellationToken);

        logger.LogInformation("Password set for pending registration {UserId}.", pending.Id);
        return Result.Success();
    }
}
