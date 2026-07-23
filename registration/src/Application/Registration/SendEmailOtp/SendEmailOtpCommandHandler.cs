using MediatR;
using Microsoft.Extensions.Logging;
using Registration.Application.Common;
using Registration.Application.Common.Interfaces;
using Registration.Domain.Common;

namespace Registration.Application.Registration.SendEmailOtp;

public sealed class SendEmailOtpCommandHandler(
    IOtpService otpService,
    IEmailSender emailSender,
    ILogger<SendEmailOtpCommandHandler> logger)
    : IRequestHandler<SendEmailOtpCommand, Result>
{
    public async Task<Result> Handle(SendEmailOtpCommand request, CancellationToken cancellationToken)
    {
        var email = EmailNormalizer.Normalize(request.Email);

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
