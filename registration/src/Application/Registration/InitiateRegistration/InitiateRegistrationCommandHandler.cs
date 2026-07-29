using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Registration.Application.Common;
using Registration.Application.Common.Interfaces;
using Registration.Application.Common.Models;
using Registration.Application.Common.Options;
using Registration.Domain.Common;
using Registration.Domain.Registration;

namespace Registration.Application.Registration.InitiateRegistration;

/// <summary>
/// Creates the pending member record. The email arrives already verified — Descope validates the OTP
/// before the app ever holds the token that authorises this call — so there is no verification state
/// to track here.
/// </summary>
/// <remarks>
/// Two repeat cases are handled deliberately, because a member who drops out mid-wizard and starts
/// again is normal rather than exceptional:
/// <list type="bullet">
///   <item>Already a full account → conflict, and the app tells them to sign in.</item>
///   <item>An unexpired pending record exists → reuse it, so a retry stays on one record instead of
///   littering the table. An expired one is replaced.</item>
/// </list>
/// This is intentionally NOT enumeration-safe: the wizard needs to tell the member their account
/// already exists. Descope has already proved the caller controls the address by this point, so
/// answering that question leaks nothing to a stranger.
/// </remarks>
public sealed class InitiateRegistrationCommandHandler(
    IPendingRegistrationRepository pendingRepository,
    IUserRegistrationRepository userRepository,
    IOptions<RegistrationOptions> options,
    TimeProvider timeProvider,
    ILogger<InitiateRegistrationCommandHandler> logger)
    : IRequestHandler<InitiateRegistrationCommand, Result<InitiateRegistrationResult>>
{
    private readonly RegistrationOptions _options = options.Value;

    public async Task<Result<InitiateRegistrationResult>> Handle(
        InitiateRegistrationCommand request,
        CancellationToken cancellationToken)
    {
        var email = EmailNormalizer.Normalize(request.Email);
        var now = timeProvider.GetUtcNow();

        if (await userRepository.EmailExistsAsync(email, cancellationToken))
        {
            return RegistrationErrors.EmailAlreadyRegistered;
        }

        var existing = await pendingRepository.GetByEmailAsync(email, cancellationToken);
        if (existing is not null && !existing.IsExpired(now))
        {
            logger.LogInformation("Resuming pending registration {UserId}.", existing.Id);
            return new InitiateRegistrationResult(
                existing.Id, existing.Email, InitiateRegistrationResult.PendingStatus);
        }

        // An expired record is dead weight — drop it so the unique-email index stays free for the
        // replacement below.
        if (existing is not null)
        {
            await pendingRepository.DeleteAsync(existing.Id, cancellationToken);
        }

        var pending = new PendingRegistration
        {
            Id = Guid.NewGuid(),
            Email = email,
            DescopeUserId = string.IsNullOrWhiteSpace(request.DescopeUserId)
                ? null
                : request.DescopeUserId,
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            DateOfBirth = request.DateOfBirth,
            ZipCode = request.ZipCode.Trim(),
            ContactNumber = string.IsNullOrWhiteSpace(request.ContactNumber)
                ? null
                : request.ContactNumber.Trim(),
            CreatedUtc = now.UtcDateTime,
            ExpiresUtc = now.AddMinutes(_options.SessionLifetimeMinutes).UtcDateTime
        };

        await pendingRepository.CreateAsync(pending, cancellationToken);

        logger.LogInformation("Pending registration {UserId} created.", pending.Id);
        return new InitiateRegistrationResult(
            pending.Id, pending.Email, InitiateRegistrationResult.PendingStatus);
    }
}
