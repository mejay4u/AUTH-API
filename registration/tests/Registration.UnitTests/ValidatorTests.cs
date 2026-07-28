using Registration.Application.Registration.CompleteRegistration;
using Registration.Application.Registration.InitiateRegistration;
using Registration.Application.Registration.SetPassword;
using Xunit;

namespace Registration.UnitTests;

public sealed class InitiateRegistrationCommandValidatorTests
{
    private readonly InitiateRegistrationCommandValidator _validator = new();

    private static InitiateRegistrationCommand Valid() =>
        new("john.doe@gmail.com", "John", "Doe", new DateOnly(1990, 5, 15), "12345");

    [Fact]
    public void Accepts_a_complete_registration()
    {
        Assert.True(_validator.Validate(Valid()).IsValid);
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-an-email")]
    [InlineData("missing@domain")]
    public void Rejects_an_invalid_email(string email)
    {
        Assert.False(_validator.Validate(Valid() with { Email = email }).IsValid);
    }

    [Theory]
    [InlineData("1234")]
    [InlineData("123456")]
    [InlineData("abcde")]
    public void Rejects_an_invalid_zip(string zip)
    {
        Assert.False(_validator.Validate(Valid() with { ZipCode = zip }).IsValid);
    }

    [Fact]
    public void Accepts_a_zip_plus_four()
    {
        Assert.True(_validator.Validate(Valid() with { ZipCode = "12345-6789" }).IsValid);
    }

    [Fact]
    public void Rejects_an_applicant_under_sixteen()
    {
        var tooYoung = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-15));
        Assert.False(_validator.Validate(Valid() with { DateOfBirth = tooYoung }).IsValid);
    }

    [Fact]
    public void Rejects_a_date_of_birth_in_the_future()
    {
        var future = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(1));
        Assert.False(_validator.Validate(Valid() with { DateOfBirth = future }).IsValid);
    }
}

public sealed class SetPasswordCommandValidatorTests
{
    private readonly SetPasswordCommandValidator _validator = new(TestDoubles.NewPasswordPolicy());

    private static SetPasswordCommand WithPassword(string password) =>
        new(Guid.NewGuid(), password, password);

    [Fact]
    public void Accepts_a_password_meeting_every_rule()
    {
        Assert.True(_validator.Validate(WithPassword("Str0ng!Pass")).IsValid);
    }

    [Theory]
    [InlineData("Sh0rt!")]        // shorter than the minimum
    [InlineData("nouppercase1!")] // no uppercase letter
    [InlineData("NoDigits!!")]    // no digit
    [InlineData("NoSpecial123")]  // no special character
    public void Rejects_a_password_breaking_a_rule(string password)
    {
        Assert.False(_validator.Validate(WithPassword(password)).IsValid);
    }

    [Fact]
    public void Rejects_a_password_longer_than_the_maximum()
    {
        Assert.False(_validator.Validate(WithPassword("Str0ng!Pass" + new string('a', 20))).IsValid);
    }

    [Fact]
    public void Rejects_a_confirmation_that_does_not_match()
    {
        var command = new SetPasswordCommand(Guid.NewGuid(), "Str0ng!Pass", "Different1!");
        Assert.False(_validator.Validate(command).IsValid);
    }

    [Fact]
    public void Rejects_an_empty_registration_id()
    {
        var command = new SetPasswordCommand(Guid.Empty, "Str0ng!Pass", "Str0ng!Pass");
        Assert.False(_validator.Validate(command).IsValid);
    }
}

public sealed class CompleteRegistrationCommandValidatorTests
{
    private readonly CompleteRegistrationCommandValidator _validator = new();

    [Theory]
    [InlineData("123-45-6789")]
    [InlineData("123456789")]
    public void Accepts_an_ssn_with_or_without_dashes(string ssn)
    {
        Assert.True(_validator.Validate(new CompleteRegistrationCommand("a@b.com", ssn, null)).IsValid);
    }

    [Theory]
    [InlineData("")]
    [InlineData("12345678")]
    [InlineData("1234567890")]
    [InlineData("123-45-678X")]
    public void Rejects_an_ssn_that_is_not_nine_digits(string ssn)
    {
        Assert.False(_validator.Validate(new CompleteRegistrationCommand("a@b.com", ssn, null)).IsValid);
    }

    [Fact]
    public void Rejects_an_invalid_email()
    {
        Assert.False(
            _validator.Validate(new CompleteRegistrationCommand("nope", "123456789", null)).IsValid);
    }
}
