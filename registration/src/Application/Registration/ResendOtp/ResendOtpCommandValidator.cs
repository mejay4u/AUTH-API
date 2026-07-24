using FluentValidation;

namespace Registration.Application.Registration.ResendOtp;

public sealed class ResendOtpCommandValidator : AbstractValidator<ResendOtpCommand>
{
    public ResendOtpCommandValidator()
    {
        RuleFor(x => x.RegistrationId)
            .NotEmpty().WithMessage("A registration session id is required.");
    }
}
