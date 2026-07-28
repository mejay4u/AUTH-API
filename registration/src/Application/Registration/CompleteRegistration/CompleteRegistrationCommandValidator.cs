using FluentValidation;

namespace Registration.Application.Registration.CompleteRegistration;

/// <summary>
/// Validates the final step's input. The SSN is accepted with or without dashes, since the flow's
/// screen may or may not format it.
/// </summary>
public sealed class CompleteRegistrationCommandValidator : AbstractValidator<CompleteRegistrationCommand>
{
    public CompleteRegistrationCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email address is required.")
            .EmailAddress().WithMessage("Enter a valid email address.")
            .MaximumLength(256);

        RuleFor(x => x.Ssn)
            .NotEmpty().WithMessage("Social Security number is required.")
            .Must(BeNineDigits).WithMessage("Enter all nine digits of your Social Security number.");

        When(x => !string.IsNullOrWhiteSpace(x.MemberId), () =>
            RuleFor(x => x.MemberId)
                .MaximumLength(50).WithMessage("Member ID is too long."));
    }

    private static bool BeNineDigits(string ssn) =>
        !string.IsNullOrWhiteSpace(ssn) && ssn.Count(char.IsDigit) == 9 && ssn.All(c => char.IsDigit(c) || c is '-' or ' ');
}
