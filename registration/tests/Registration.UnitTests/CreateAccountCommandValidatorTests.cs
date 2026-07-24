using Microsoft.Extensions.Options;
using Registration.Application.Common.Options;
using Registration.Application.Registration.CreateAccount;
using Xunit;

namespace Registration.UnitTests;

public sealed class CreateAccountCommandValidatorTests
{
    private static CreateAccountCommandValidator CreateValidator(PasswordPolicyOptions? policy = null) =>
        new(Options.Create(policy ?? new PasswordPolicyOptions()));

    private static CreateAccountCommand Command(string? password = null, string? confirm = null, Guid? id = null)
    {
        var pwd = password ?? "Str0ng!Pass";
        return new CreateAccountCommand(id ?? Guid.NewGuid(), pwd, confirm ?? pwd);
    }

    [Fact]
    public void Valid_password_and_matching_confirmation_passes()
    {
        Assert.True(CreateValidator().Validate(Command()).IsValid);
    }

    [Theory]
    [InlineData("Ab1!")]              // too short
    [InlineData("alllowercase1!")]    // no uppercase
    [InlineData("NoDigitsHere!")]     // no digit
    [InlineData("NoSpecial123")]      // no special character
    public void Password_violating_the_policy_is_rejected(string password)
    {
        var result = CreateValidator().Validate(Command(password: password));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateAccountCommand.Password));
    }

    [Fact]
    public void Mismatched_confirmation_is_rejected()
    {
        var result = CreateValidator().Validate(Command(password: "Str0ng!Pass", confirm: "Different1!"));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateAccountCommand.ConfirmPassword));
    }

    [Fact]
    public void Empty_registration_id_is_rejected()
    {
        var result = CreateValidator().Validate(Command(id: Guid.Empty));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateAccountCommand.RegistrationId));
    }
}
