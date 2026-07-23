using FluentValidation;

namespace Registration.Application.Registration.SendEmailOtp;

public sealed class SendEmailOtpCommandValidator : AbstractValidator<SendEmailOtpCommand>
{
    public SendEmailOtpCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email address is required.")
            .EmailAddress().WithMessage("Enter a valid email address.")
            .MaximumLength(256);
    }
}
