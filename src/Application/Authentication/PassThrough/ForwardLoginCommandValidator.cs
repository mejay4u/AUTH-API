using FluentValidation;

namespace AuthApi.Application.Authentication.PassThrough;

/// <summary>
/// A deliberately thin shape check. The BFA is not the authority on what a valid credential looks
/// like — ARTS is — so this only rejects payloads that could not possibly succeed, sparing an
/// upstream round trip (and an entry in ARTS's failed-login counters) for empty submits and probes.
/// Anything that could plausibly be real goes upstream untouched.
/// </summary>
public sealed class ForwardLoginCommandValidator : AbstractValidator<ForwardLoginCommand>
{
    public ForwardLoginCommandValidator()
    {
        // Property names are overridden to the wire names so the 400 body speaks the client's
        // contract ("password"), not the command's internal shape ("HasPassword").
        RuleFor(x => x.Username)
            .NotEmpty().WithMessage("'username' is required.")
            .OverridePropertyName("username");

        RuleFor(x => x.Lob)
            .NotEmpty().WithMessage("'lob' is required.")
            .OverridePropertyName("lob");

        RuleFor(x => x.HasPassword)
            .Equal(true).WithMessage("'password' is required.")
            .OverridePropertyName("password");
    }
}
