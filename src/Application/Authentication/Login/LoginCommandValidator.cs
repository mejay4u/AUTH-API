using FluentValidation;

namespace AuthApi.Application.Authentication.Login;

public sealed class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.Username)
            .NotEmpty()
            .MaximumLength(256);

        RuleFor(x => x.Password)
            .NotEmpty()
            .MaximumLength(256); // upper bound guards against hashing-based DoS with huge inputs

        RuleFor(x => x.Lob)
            .NotEmpty()
            .MaximumLength(64); // selects which LOB database to authenticate against
    }
}
