using FluentValidation;

namespace Registration.Application.Registration.StartRegistration;

/// <summary>
/// Server-side, authoritative validation of the personal information collected on the registration
/// screen. Messages state exactly what is required. Applicants must be at least <see cref="MinimumAgeYears"/>.
/// </summary>
public sealed class StartRegistrationCommandValidator : AbstractValidator<StartRegistrationCommand>
{
    /// <summary>Minimum age to register.</summary>
    private const int MinimumAgeYears = 16;

    public StartRegistrationCommandValidator()
    {
        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("First name is required.")
            .MaximumLength(100);

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("Last name is required.")
            .MaximumLength(100);

        RuleFor(x => x.DateOfBirth)
            .Cascade(CascadeMode.Stop)
            .Must(BeAValidDateOfBirth)
                .WithMessage("Enter a valid date of birth (a real date in the past).")
            .Must(BeAtLeastMinimumAge)
                .WithMessage($"You must be at least {MinimumAgeYears} years old to register.");

        RuleFor(x => x.ZipCode)
            .NotEmpty().WithMessage("ZIP code is required.")
            .Matches(@"^\d{5}(-\d{4})?$").WithMessage("Enter a valid ZIP code (12345 or 12345-6789).");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email address is required.")
            .EmailAddress().WithMessage("Enter a valid email address.")
            .MaximumLength(256);

        When(x => !string.IsNullOrWhiteSpace(x.ContactNumber), () =>
            RuleFor(x => x.ContactNumber)
                .Matches(@"^[0-9+()\-\s]{7,20}$").WithMessage("Enter a valid contact number."));
    }

    private static bool BeAValidDateOfBirth(DateOnly dateOfBirth) =>
        dateOfBirth != default
        && dateOfBirth.Year >= 1900
        && dateOfBirth < DateOnly.FromDateTime(DateTime.UtcNow);

    private static bool BeAtLeastMinimumAge(DateOnly dateOfBirth)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var age = today.Year - dateOfBirth.Year;
        if (dateOfBirth > today.AddYears(-age))
        {
            age--; // birthday hasn't occurred yet this year
        }

        return age >= MinimumAgeYears;
    }
}
