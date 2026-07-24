using MediatR;
using Registration.Application.Common.Interfaces;
using Registration.Domain.Common;

namespace Registration.Application.Registration.ResendOtp;

/// <summary>
/// Re-issues the OTP for a registration session. Enforces the cooldown / per-email cap via the OTP
/// service. Returns a generic success for an unknown/expired session so it doesn't reveal which
/// session ids are valid.
/// </summary>
public sealed class ResendOtpCommandHandler(
    IPendingRegistrationRepository pendingRepository,
    IUserRegistrationRepository userRepository,
    IOtpService otpService,
    IEmailSender emailSender,
    TimeProvider timeProvider)
    : IRequestHandler<ResendOtpCommand, Result>
{
    public async Task<Result> Handle(ResendOtpCommand request, CancellationToken cancellationToken)
    {
        var session = await pendingRepository.GetAsync(request.RegistrationId, cancellationToken);
        if (session is null || session.IsExpired(timeProvider.GetUtcNow()))
        {
            return Result.Success();
        }

        // Enumeration-safe: don't mail codes to an address that already has an account.
        if (await userRepository.EmailExistsAsync(session.Email, cancellationToken))
        {
            return Result.Success();
        }

        var issued = await otpService.IssueAsync(session.Email, cancellationToken);
        if (issued.IsFailure)
        {
            return Result.Failure(issued.Error);
        }

        await emailSender.SendOtpAsync(session.Email, issued.Value.Code, issued.Value.ExpiresUtc, cancellationToken);
        return Result.Success();
    }
}
