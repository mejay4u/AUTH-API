using FluentValidation;

namespace Registration.Application.Registration.SyncDescopeUser;

public sealed class SyncDescopeUserCommandValidator : AbstractValidator<SyncDescopeUserCommand>
{
    public SyncDescopeUserCommandValidator()
    {
        RuleFor(x => x.DescopeUserId)
            .NotEmpty().WithMessage("Descope user id is required.")
            .MaximumLength(128);

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email address is required.")
            .EmailAddress().WithMessage("Enter a valid email address.")
            .MaximumLength(256);

        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
    }
}
