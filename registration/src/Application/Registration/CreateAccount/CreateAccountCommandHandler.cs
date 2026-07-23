using MediatR;
using Microsoft.Extensions.Logging;
using Registration.Application.Common;
using Registration.Application.Common.Interfaces;
using Registration.Application.Common.Models;
using Registration.Domain.Common;
using Registration.Domain.Registration;

namespace Registration.Application.Registration.CreateAccount;

/// <summary>
/// Creates the portal user in the registration database. Enforces (in order): the email must be
/// OTP-verified, the email must not already exist (duplicate check), then hashes the password with the
/// configured best-practice scheme and inserts the user — email as username — with the personal
/// information from the registration screen.
/// </summary>
public sealed class CreateAccountCommandHandler(
    IOtpService otpService,
    IUserRegistrationRepository repository,
    IPasswordHasher passwordHasher,
    ILogger<CreateAccountCommandHandler> logger)
    : IRequestHandler<CreateAccountCommand, Result<CreateAccountResult>>
{
    public async Task<Result<CreateAccountResult>> Handle(
        CreateAccountCommand request,
        CancellationToken cancellationToken)
    {
        var email = EmailNormalizer.Normalize(request.Email);

        if (!await otpService.IsVerifiedAsync(email, cancellationToken))
        {
            return RegistrationErrors.EmailNotVerified;
        }

        if (await repository.EmailExistsAsync(email, cancellationToken))
        {
            return RegistrationErrors.EmailAlreadyRegistered;
        }

        var (hash, salt) = passwordHasher.Hash(request.Password);

        var userId = await repository.CreateUserAsync(
            new NewUserRegistration(
                Email: email,
                Username: email,
                PasswordHash: hash,
                PasswordSalt: salt,
                FirstName: request.FirstName.Trim(),
                LastName: request.LastName.Trim(),
                DateOfBirth: request.DateOfBirth,
                ZipCode: request.ZipCode.Trim(),
                ContactNumber: string.IsNullOrWhiteSpace(request.ContactNumber) ? null : request.ContactNumber.Trim()),
            cancellationToken);

        // One-time use: clear the verification so the same verified state can't create another account.
        await otpService.ConsumeVerifiedAsync(email, cancellationToken);

        logger.LogInformation("Created portal user {UserId}.", userId);
        return new CreateAccountResult(userId, email, email);
    }
}
