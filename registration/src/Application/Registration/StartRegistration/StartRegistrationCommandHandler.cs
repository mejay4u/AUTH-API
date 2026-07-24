using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Registration.Application.Common;
using Registration.Application.Common.Interfaces;
using Registration.Application.Common.Models;
using Registration.Application.Common.Options;
using Registration.Domain.Common;
using Registration.Domain.Registration;

namespace Registration.Application.Registration.StartRegistration;

/// <summary>
/// Opens a registration session: persists the personal information as a <see cref="PendingRegistration"/>
/// and emails a verification code. Enumeration-safe — a session id is always returned, but a code is
/// issued only when the email is not already registered, so the response never reveals existence.
/// </summary>
public sealed class StartRegistrationCommandHandler(
    IPendingRegistrationRepository pendingRepository,
    IUserRegistrationRepository userRepository,
    IOtpService otpService,
    IEmailSender emailSender,
    IOptions<RegistrationOptions> options,
    TimeProvider timeProvider,
    ILogger<StartRegistrationCommandHandler> logger)
    : IRequestHandler<StartRegistrationCommand, Result<StartRegistrationResult>>
{
    private readonly RegistrationOptions _options = options.Value;

    public async Task<Result<StartRegistrationResult>> Handle(
        StartRegistrationCommand request,
        CancellationToken cancellationToken)
    {
        var email = EmailNormalizer.Normalize(request.Email);
        var now = timeProvider.GetUtcNow();

        var session = new PendingRegistration
        {
            Id = Guid.NewGuid(),
            Email = email,
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            DateOfBirth = request.DateOfBirth,
            ZipCode = request.ZipCode.Trim(),
            ContactNumber = string.IsNullOrWhiteSpace(request.ContactNumber) ? null : request.ContactNumber.Trim(),
            EmailVerified = false,
            CreatedUtc = now.UtcDateTime,
            ExpiresUtc = now.AddMinutes(_options.SessionLifetimeMinutes).UtcDateTime
        };

        await pendingRepository.CreateAsync(session, cancellationToken);

        // Enumeration-safe: only mail a code when the email is not already registered.
        if (!await userRepository.EmailExistsAsync(email, cancellationToken))
        {
            var issued = await otpService.IssueAsync(email, cancellationToken);
            if (issued.IsSuccess)
            {
                await emailSender.SendOtpAsync(email, issued.Value.Code, issued.Value.ExpiresUtc, cancellationToken);
            }
            else
            {
                // Cooldown / per-email cap hit (e.g. the same email started repeatedly). The session is
                // still created; the user can resend once the cooldown elapses.
                logger.LogInformation("Registration started but OTP not issued: {Reason}.", issued.Error.Code);
            }
        }

        logger.LogInformation("Registration session {RegistrationId} started.", session.Id);
        return new StartRegistrationResult(session.Id);
    }
}
