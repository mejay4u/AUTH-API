using FluentValidation;

namespace AuthApi.Application.Sso.GetSso;

public sealed class GetSsoQueryValidator : AbstractValidator<GetSsoQuery>
{
    public GetSsoQueryValidator()
    {
        RuleFor(x => x.Lob)
            .NotEmpty()
            .MaximumLength(64);

        RuleFor(x => x.SsoName)
            .NotEmpty()
            .MaximumLength(64);

        RuleFor(x => x.PlanCode)
            .MaximumLength(64);

        RuleFor(x => x.Member.MemberId)
            .NotEmpty()
            .WithName("MemberId"); // comes from the JWT 'sub' claim, not the caller's input
    }
}
