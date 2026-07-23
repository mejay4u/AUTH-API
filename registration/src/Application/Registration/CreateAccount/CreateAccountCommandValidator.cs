using FluentValidation;
using Microsoft.Extensions.Options;
using Registration.Application.Common.Options;

namespace Registration.Application.Registration.CreateAccount;

/// <summary>
/// Server-side, authoritative validation for account creation. Reads the configurable
/// <see cref="PasswordPolicyOptions"/> so the password rules change via config, never code. Every field
/// from the registration screen is validated here; messages state exactly what is required.
/// </summary>
public sealed class CreateAccountCommandValidator : AbstractValidator<CreateAccountCommand>
{
    public CreateAccountCommandValidator(IOptions<PasswordPolicyOptions> options)
    {
        var policy = options.Value;

        // --- Personal information ---
        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("First name is required.")
            .MaximumLength(100);

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("Last name is required.")
            .MaximumLength(100);

        RuleFor(x => x.DateOfBirth)
            .Must(BeAValidDateOfBirth)
            .WithMessage("Enter a valid date of birth (a real date in the past).");

        RuleFor(x => x.ZipCode)
            .NotEmpty().WithMessage("ZIP code is required.")
            .Matches(@"^\d{5}(-\d{4})?$").WithMessage("Enter a valid ZIP code (12345 or 12345-6789).");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email address is required.")
            .EmailAddress().WithMessage("Enter a valid email address.")
            .MaximumLength(256);

        // Contact number is optional; validate only when supplied.
        When(x => !string.IsNullOrWhiteSpace(x.ContactNumber), () =>
            RuleFor(x => x.ContactNumber)
                .Matches(@"^[0-9+()\-\s]{7,20}$").WithMessage("Enter a valid contact number."));

        // --- Password ---
        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required.")
            .MinimumLength(policy.MinLength)
                .WithMessage($"Password must be at least {policy.MinLength} characters.")
            .MaximumLength(policy.MaxLength)
                .WithMessage($"Password must be no more than {policy.MaxLength} characters.");

        if (policy.RequireUppercase)
        {
            RuleFor(x => x.Password)
                .Matches("[A-Z]")
                .WithMessage("Password must contain at least one uppercase letter (A-Z).");
        }

        if (policy.RequireDigit)
        {
            RuleFor(x => x.Password)
                .Matches("[0-9]")
                .WithMessage("Password must contain at least one number (0-9).");
        }

        if (policy.RequireSpecialCharacter)
        {
            var special = policy.AllowedSpecialCharacters;
            RuleFor(x => x.Password)
                .Must(password => !string.IsNullOrEmpty(password) && password.Any(special.Contains))
                .WithMessage($"Password must contain at least one special character (e.g. {special}).");
        }

        RuleFor(x => x.ConfirmPassword)
            .Equal(x => x.Password)
            .WithMessage("Passwords do not match.");
    }

    private static bool BeAValidDateOfBirth(DateOnly dateOfBirth) =>
        dateOfBirth != default
        && dateOfBirth.Year >= 1900
        && dateOfBirth < DateOnly.FromDateTime(DateTime.UtcNow);
}
