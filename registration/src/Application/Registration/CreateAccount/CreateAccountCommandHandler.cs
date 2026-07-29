using MediatR;
using Microsoft.Extensions.Logging;
using Registration.Application.Common.Interfaces;
using Registration.Application.Common.Models;
using Registration.Domain.Common;
using Registration.Domain.Registration;

namespace Registration.Application.Registration.CreateAccount;

/// <summary>
/// Promotes a pending registration into a real portal user, then deletes it.
///
/// Enforces, in order: the pending record must exist and be unexpired, and the
/// email must not already be registered. The password is hashed with the
/// configured scheme; the personal information is taken from the pending record
/// (the server, not the client), so a tampered request can't change the name or
/// date of birth that were reviewed.
/// </summary>
/// <remarks>
/// There is no email-verified check. Descope verifies the address before the
/// app is ever given the session token that authorises these calls, so a
/// pending record existing at all means the address was verified.
/// </remarks>
public sealed class CreateAccountCommandHandler(
    IPendingRegistrationRepository pendingRepository,
    IUserRegistrationRepository userRepository,
    IPasswordHasher passwordHasher,
    TimeProvider timeProvider,
    ILogger<CreateAccountCommandHandler> logger)
    : IRequestHandler<CreateAccountCommand, Result<CreateAccountResult>>
{
    public async Task<Result<CreateAccountResult>> Handle(
        CreateAccountCommand request,
        CancellationToken cancellationToken)
    {
        var pending = await pendingRepository.GetAsync(request.UserId, cancellationToken);
        if (pending is null || pending.IsExpired(timeProvider.GetUtcNow()))
        {
            return RegistrationErrors.RegistrationNotFoundOrExpired;
        }

        if (await userRepository.EmailExistsAsync(pending.Email, cancellationToken))
        {
            return RegistrationErrors.EmailAlreadyRegistered;
        }

        var (hash, salt) = passwordHasher.Hash(request.Password);

        var userId = await userRepository.CreateUserAsync(
            new NewUserRegistration(
                // The pending record's id carries over, so the identifier the
                // app has been holding since the first call stays valid.
                Id: pending.Id,
                Email: pending.Email,
                Username: pending.Email,
                PasswordHash: hash,
                PasswordSalt: salt,
                FirstName: pending.FirstName,
                LastName: pending.LastName,
                DateOfBirth: pending.DateOfBirth,
                ZipCode: pending.ZipCode,
                ContactNumber: pending.ContactNumber),
            cancellationToken);

        // The pending record is single-use: remove it once the account exists.
        await pendingRepository.DeleteAsync(pending.Id, cancellationToken);

        logger.LogInformation("Created portal user {UserId} from pending registration.", userId);
        return new CreateAccountResult(userId, pending.Email, pending.Email);
    }
}
