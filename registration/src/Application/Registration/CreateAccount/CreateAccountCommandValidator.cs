using FluentValidation;
using Microsoft.Extensions.Options;
using Registration.Application.Common.Options;

namespace Registration.Application.Registration.CreateAccount;

/// <summary>
/// Server-side, authoritative validation for account creation. Reads the configurable
/// <see cref="PasswordPolicyOptions"/> so the password rules change via config, never code. Personal
/// information was validated when the session was opened (StartRegistration).
/// </summary>
public sealed class CreateAccountCommandValidator : AbstractValidator<CreateAccountCommand>
{
    public CreateAccountCommandValidator(IOptions<PasswordPolicyOptions> options)
    {
        var policy = options.Value;

        RuleFor(x => x.RegistrationId)
            .NotEmpty().WithMessage("A registration session id is required.");

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
}
