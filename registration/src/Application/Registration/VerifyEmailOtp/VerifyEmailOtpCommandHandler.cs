using MediatR;
using Registration.Application.Common.Interfaces;
using Registration.Domain.Common;
using Registration.Domain.Registration;

namespace Registration.Application.Registration.VerifyEmailOtp;

/// <summary>
/// Verifies the OTP for a registration session and, on success, marks the session's email verified.
/// </summary>
public sealed class VerifyEmailOtpCommandHandler(
    IPendingRegistrationRepository pendingRepository,
    IOtpService otpService,
    TimeProvider timeProvider)
    : IRequestHandler<VerifyEmailOtpCommand, Result>
{
    public async Task<Result> Handle(VerifyEmailOtpCommand request, CancellationToken cancellationToken)
    {
        var session = await pendingRepository.GetAsync(request.RegistrationId, cancellationToken);
        if (session is null || session.IsExpired(timeProvider.GetUtcNow()))
        {
            return Result.Failure(RegistrationErrors.SessionNotFoundOrExpired);
        }

        var result = await otpService.VerifyAsync(session.Email, request.Code, cancellationToken);
        if (result.IsFailure)
        {
            return result;
        }

        await pendingRepository.MarkEmailVerifiedAsync(session.Id, cancellationToken);
        return Result.Success();
    }
}
