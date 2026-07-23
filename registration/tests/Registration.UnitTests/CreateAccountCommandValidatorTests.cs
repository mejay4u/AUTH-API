using Microsoft.Extensions.Options;
using Registration.Application.Common.Options;
using Registration.Application.Registration.CreateAccount;
using Xunit;

namespace Registration.UnitTests;

public sealed class CreateAccountCommandValidatorTests
{
    private static CreateAccountCommandValidator CreateValidator(PasswordPolicyOptions? policy = null) =>
        new(Options.Create(policy ?? new PasswordPolicyOptions()));

    private static CreateAccountCommand ValidCommand(
        string? password = null,
        string? confirm = null,
        string email = "john.doe@gmail.com",
        string firstName = "John",
        string lastName = "Doe",
        string zip = "12345",
        string? contact = "123-456-7890",
        DateOnly? dob = null)
    {
        var pwd = password ?? "Str0ng!Pass";
        return new CreateAccountCommand(
            firstName,
            lastName,
            dob ?? new DateOnly(1990, 5, 15),
            zip,
            email,
            contact,
            pwd,
            confirm ?? pwd);
    }

    [Fact]
    public void Fully_valid_registration_passes()
    {
        Assert.True(CreateValidator().Validate(ValidCommand()).IsValid);
    }

    [Theory]
    [InlineData("Ab1!")]              // too short
    [InlineData("alllowercase1!")]    // no uppercase
    [InlineData("NoDigitsHere!")]     // no digit
    [InlineData("NoSpecial123")]      // no special character
    public void Password_violating_the_policy_is_rejected(string password)
    {
        var result = CreateValidator().Validate(ValidCommand(password: password));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateAccountCommand.Password));
    }

    [Fact]
    public void Mismatched_confirmation_is_rejected()
    {
        var result = CreateValidator().Validate(ValidCommand(password: "Str0ng!Pass", confirm: "Different1!"));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateAccountCommand.ConfirmPassword));
    }

    [Fact]
    public void Invalid_email_is_rejected()
    {
        var result = CreateValidator().Validate(ValidCommand(email: "not-an-email"));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateAccountCommand.Email));
    }

    [Fact]
    public void Missing_first_name_is_rejected()
    {
        var result = CreateValidator().Validate(ValidCommand(firstName: ""));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateAccountCommand.FirstName));
    }

    [Theory]
    [InlineData("1234")]      // too short
    [InlineData("abcde")]     // not numeric
    public void Invalid_zip_code_is_rejected(string zip)
    {
        var result = CreateValidator().Validate(ValidCommand(zip: zip));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateAccountCommand.ZipCode));
    }

    [Fact]
    public void Future_date_of_birth_is_rejected()
    {
        var future = DateOnly.FromDateTime(DateTime.UtcNow).AddYears(1);

        var result = CreateValidator().Validate(ValidCommand(dob: future));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateAccountCommand.DateOfBirth));
    }

    [Fact]
    public void Missing_contact_number_is_allowed()
    {
        Assert.True(CreateValidator().Validate(ValidCommand(contact: null)).IsValid);
    }
}
