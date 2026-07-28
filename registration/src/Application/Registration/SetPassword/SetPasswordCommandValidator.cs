using FluentValidation;
using Microsoft.Extensions.Options;
using Registration.Application.Common.Options;

namespace Registration.Application.Registration.SetPassword;

/// <summary>
/// Server-side, authoritative password validation. Reads the configurable
/// <see cref="PasswordPolicyOptions"/> so the rules change via config, never code — and so the
/// checklist the app shows while typing can be kept honest against the same source.
/// </summary>
public sealed class SetPasswordCommandValidator : AbstractValidator<SetPasswordCommand>
{
    public SetPasswordCommandValidator(IOptions<PasswordPolicyOptions> options)
    {
        var policy = options.Value;

        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("A registration id is required.");

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
