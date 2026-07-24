using Registration.Application.Registration.StartRegistration;
using Xunit;

namespace Registration.UnitTests;

public sealed class StartRegistrationCommandValidatorTests
{
    private static readonly StartRegistrationCommandValidator Validator = new();

    private static StartRegistrationCommand ValidCommand(
        string email = "john.doe@gmail.com",
        string firstName = "John",
        string lastName = "Doe",
        string zip = "12345",
        string? contact = "123-456-7890",
        DateOnly? dob = null) =>
        new(firstName, lastName, dob ?? new DateOnly(1990, 5, 15), zip, email, contact);

    [Fact]
    public void Fully_valid_personal_information_passes()
    {
        Assert.True(Validator.Validate(ValidCommand()).IsValid);
    }

    [Fact]
    public void Missing_first_name_is_rejected()
    {
        var result = Validator.Validate(ValidCommand(firstName: ""));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(StartRegistrationCommand.FirstName));
    }

    [Theory]
    [InlineData("1234")]
    [InlineData("abcde")]
    public void Invalid_zip_code_is_rejected(string zip)
    {
        var result = Validator.Validate(ValidCommand(zip: zip));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(StartRegistrationCommand.ZipCode));
    }

    [Fact]
    public void Invalid_email_is_rejected()
    {
        var result = Validator.Validate(ValidCommand(email: "not-an-email"));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(StartRegistrationCommand.Email));
    }

    [Fact]
    public void Missing_contact_number_is_allowed()
    {
        Assert.True(Validator.Validate(ValidCommand(contact: null)).IsValid);
    }

    [Fact]
    public void Future_date_of_birth_is_rejected()
    {
        var future = DateOnly.FromDateTime(DateTime.UtcNow).AddYears(1);

        var result = Validator.Validate(ValidCommand(dob: future));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(StartRegistrationCommand.DateOfBirth));
    }

    [Fact]
    public void Applicant_under_16_is_rejected()
    {
        var fifteen = DateOnly.FromDateTime(DateTime.UtcNow).AddYears(-15);

        var result = Validator.Validate(ValidCommand(dob: fifteen));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(StartRegistrationCommand.DateOfBirth));
    }

    [Fact]
    public void Applicant_exactly_16_is_allowed()
    {
        var sixteenToday = DateOnly.FromDateTime(DateTime.UtcNow).AddYears(-16);

        Assert.True(Validator.Validate(ValidCommand(dob: sixteenToday)).IsValid);
    }
}
