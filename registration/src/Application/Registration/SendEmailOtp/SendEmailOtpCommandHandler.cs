using MediatR;
using Microsoft.Extensions.Logging;
using Registration.Application.Common;
using Registration.Application.Common.Interfaces;
using Registration.Domain.Common;

namespace Registration.Application.Registration.SendEmailOtp;

public sealed class SendEmailOtpCommandHandler(
    IUserRegistrationRepository repository,
    IOtpService otpService,
    IEmailSender emailSender,
    ILogger<SendEmailOtpCommandHandler> logger)
    : IRequestHandler<SendEmailOtpCommand, Result>
{
    public async Task<Result> Handle(SendEmailOtpCommand request, CancellationToken cancellationToken)
    {
        var email = EmailNormalizer.Normalize(request.Email);

        // Best practice: don't start the verification flow for an email that already has an account, and
        // don't reveal whether it exists. Return the SAME generic success either way (enumeration-safe)
        // and simply skip issuing a code — so we never mail OTPs to already-registered addresses.
        if (await repository.EmailExistsAsync(email, cancellationToken))
        {
            logger.LogInformation("OTP requested for an already-registered email; no code issued.");
            return Result.Success();
        }

        var issued = await otpService.IssueAsync(email, cancellationToken);
        if (issued.IsFailure)
        {
            // Cooldown / per-email cap reached — surfaced as a 409/429 by the API layer.
            logger.LogInformation("Email OTP not issued: {Reason}.", issued.Error.Code);
            return Result.Failure(issued.Error);
        }

        await emailSender.SendOtpAsync(email, issued.Value.Code, issued.Value.ExpiresUtc, cancellationToken);

        // Never log the email address or the code.
        logger.LogInformation("Email OTP issued and dispatched for a registration request.");
        return Result.Success();
    }
}
