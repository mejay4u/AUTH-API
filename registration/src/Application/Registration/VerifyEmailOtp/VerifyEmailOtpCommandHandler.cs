using MediatR;
using Registration.Application.Common;
using Registration.Application.Common.Interfaces;
using Registration.Domain.Common;

namespace Registration.Application.Registration.VerifyEmailOtp;

public sealed class VerifyEmailOtpCommandHandler(IOtpService otpService)
    : IRequestHandler<VerifyEmailOtpCommand, Result>
{
    public Task<Result> Handle(VerifyEmailOtpCommand request, CancellationToken cancellationToken)
    {
        var email = EmailNormalizer.Normalize(request.Email);
        return otpService.VerifyAsync(email, request.Code, cancellationToken);
    }
}
