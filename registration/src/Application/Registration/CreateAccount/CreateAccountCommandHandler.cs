using MediatR;
using Microsoft.Extensions.Logging;
using Registration.Application.Common.Interfaces;
using Registration.Application.Common.Models;
using Registration.Domain.Common;
using Registration.Domain.Registration;

namespace Registration.Application.Registration.CreateAccount;

/// <summary>
/// Promotes a verified registration session into a real portal user, then deletes the session.
/// Enforces (in order): the session must exist and be unexpired, its email must be verified, and the
/// email must not already be registered. The password is hashed with the configured best-practice
/// scheme; the personal information is taken from the session (the server, not the client).
/// </summary>
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
        var session = await pendingRepository.GetAsync(request.RegistrationId, cancellationToken);
        if (session is null || session.IsExpired(timeProvider.GetUtcNow()))
        {
            return RegistrationErrors.SessionNotFoundOrExpired;
        }

        if (!session.EmailVerified)
        {
            return RegistrationErrors.EmailNotVerified;
        }

        if (await userRepository.EmailExistsAsync(session.Email, cancellationToken))
        {
            return RegistrationErrors.EmailAlreadyRegistered;
        }

        var (hash, salt) = passwordHasher.Hash(request.Password);

        var userId = await userRepository.CreateUserAsync(
            new NewUserRegistration(
                Email: session.Email,
                Username: session.Email,
                PasswordHash: hash,
                PasswordSalt: salt,
                FirstName: session.FirstName,
                LastName: session.LastName,
                DateOfBirth: session.DateOfBirth,
                ZipCode: session.ZipCode,
                ContactNumber: session.ContactNumber),
            cancellationToken);

        // The session is single-use: remove it once the account exists.
        await pendingRepository.DeleteAsync(session.Id, cancellationToken);

        logger.LogInformation("Created portal user {UserId} from registration session {RegistrationId}.",
            userId, session.Id);
        return new CreateAccountResult(userId, session.Email, session.Email);
    }
}
