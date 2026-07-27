using Registration.Application.Registration.SyncDescopeUser;
using Registration.Application.Registration.VerifyLegacyLogin;
using Xunit;

namespace Registration.UnitTests;

public sealed class ValidatorsTests
{
    private static readonly SyncDescopeUserCommandValidator SyncValidator = new();
    private static readonly VerifyLegacyLoginCommandValidator VerifyValidator = new();

    [Fact]
    public void Sync_valid_payload_passes()
    {
        var command = new SyncDescopeUserCommand("D-1", "a@b.com", "A", "B", null, null, null);
        Assert.True(SyncValidator.Validate(command).IsValid);
    }

    [Fact]
    public void Sync_missing_descope_id_is_rejected()
    {
        var command = new SyncDescopeUserCommand("", "a@b.com", "A", "B", null, null, null);
        Assert.False(SyncValidator.Validate(command).IsValid);
    }

    [Fact]
    public void Sync_invalid_email_is_rejected()
    {
        var command = new SyncDescopeUserCommand("D-1", "not-an-email", "A", "B", null, null, null);
        Assert.False(SyncValidator.Validate(command).IsValid);
    }

    [Fact]
    public void Verify_valid_payload_passes()
    {
        Assert.True(VerifyValidator.Validate(new VerifyLegacyLoginCommand("a@b.com", "pw", null)).IsValid);
    }

    [Fact]
    public void Verify_missing_password_is_rejected()
    {
        Assert.False(VerifyValidator.Validate(new VerifyLegacyLoginCommand("a@b.com", "", null)).IsValid);
    }

    [Fact]
    public void Verify_invalid_email_is_rejected()
    {
        Assert.False(VerifyValidator.Validate(new VerifyLegacyLoginCommand("nope", "pw", null)).IsValid);
    }
}
