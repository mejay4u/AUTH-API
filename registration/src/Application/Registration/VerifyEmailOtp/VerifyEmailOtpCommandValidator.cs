using FluentValidation;
using Microsoft.Extensions.Options;
using Registration.Application.Common.Options;

namespace Registration.Application.Registration.VerifyEmailOtp;

public sealed class VerifyEmailOtpCommandValidator : AbstractValidator<VerifyEmailOtpCommand>
{
    public VerifyEmailOtpCommandValidator(IOptions<OtpOptions> options)
    {
        var codeLength = options.Value.CodeLength;

        RuleFor(x => x.RegistrationId)
            .NotEmpty().WithMessage("A registration session id is required.");

        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Verification code is required.")
            .Length(codeLength).WithMessage($"The verification code must be {codeLength} digits.")
            .Matches("^[0-9]+$").WithMessage("The verification code must contain digits only.");
    }
}
