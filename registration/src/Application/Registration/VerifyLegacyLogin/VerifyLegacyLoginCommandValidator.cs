using FluentValidation;

namespace Registration.Application.Registration.VerifyLegacyLogin;

public sealed class VerifyLegacyLoginCommandValidator : AbstractValidator<VerifyLegacyLoginCommand>
{
    public VerifyLegacyLoginCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email address is required.")
            .EmailAddress().WithMessage("Enter a valid email address.")
            .MaximumLength(256);

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required.")
            .MaximumLength(256); // upper bound guards against oversized inputs
    }
}
