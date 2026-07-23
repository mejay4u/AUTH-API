using Microsoft.Extensions.Options;
using Registration.Application.Common.Options;
using Registration.Application.Registration.CreateAccount;
using Xunit;

namespace Registration.UnitTests;

public sealed class CreateAccountCommandValidatorTests
{
    private static CreateAccountCommandValidator CreateValidator(PasswordPolicyOptions? policy = null) =>
        new(Options.Create(policy ?? new PasswordPolicyOptions()));

    [Fact]
    public void Valid_password_and_matching_confirmation_passes()
    {
        var validator = CreateValidator();
        var command = new CreateAccountCommand("john.doe@gmail.com", "Str0ng!Pass", "Str0ng!Pass");

        var result = validator.Validate(command);

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("Ab1!")]              // too short
    [InlineData("alllowercase1!")]    // no uppercase
    [InlineData("NoDigitsHere!")]     // no digit
    [InlineData("NoSpecial123")]      // no special character
    public void Password_violating_the_policy_is_rejected(string password)
    {
        var validator = CreateValidator();
        var command = new CreateAccountCommand("john.doe@gmail.com", password, password);

        var result = validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateAccountCommand.Password));
    }

    [Fact]
    public void Mismatched_confirmation_is_rejected()
    {
        var validator = CreateValidator();
        var command = new CreateAccountCommand("john.doe@gmail.com", "Str0ng!Pass", "Different1!");

        var result = validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateAccountCommand.ConfirmPassword));
    }

    [Fact]
    public void Password_over_the_configured_maximum_is_rejected()
    {
        var validator = CreateValidator(new PasswordPolicyOptions { MinLength = 8, MaxLength = 20 });
        var tooLong = "Str0ng!" + new string('a', 30);
        var command = new CreateAccountCommand("john.doe@gmail.com", tooLong, tooLong);

        var result = validator.Validate(command);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Invalid_email_is_rejected()
    {
        var validator = CreateValidator();
        var command = new CreateAccountCommand("not-an-email", "Str0ng!Pass", "Str0ng!Pass");

        var result = validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateAccountCommand.Email));
    }
}
