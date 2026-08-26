using FluentValidation;

namespace AuthApi.Application.Authentication.PassThrough;

/// <summary>Rejects an empty refresh submit before it costs an upstream round trip.</summary>
public sealed class ForwardRefreshCommandValidator : AbstractValidator<ForwardRefreshCommand>
{
    public ForwardRefreshCommandValidator()
    {
        RuleFor(x => x.HasRefreshToken)
            .Equal(true).WithMessage("'refreshToken' is required.")
            .OverridePropertyName("refreshToken");
    }
}
