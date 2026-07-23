using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Registration.Application.Common.Options;
using Registration.Domain.Registration;
using Registration.Infrastructure.Otp;
using Xunit;

namespace Registration.UnitTests;

public sealed class OtpServiceTests
{
    private const string Email = "john.doe@gmail.com";

    private static (OtpService Service, TestTimeProvider Clock) Create(OtpOptions? options = null)
    {
        var clock = new TestTimeProvider();
        var cache = new MemoryCache(new MemoryCacheOptions());
        var service = new OtpService(cache, Options.Create(options ?? new OtpOptions()), clock);
        return (service, clock);
    }

    [Fact]
    public async Task Issue_then_verify_with_the_correct_code_succeeds_and_marks_verified()
    {
        var (service, _) = Create();

        var issued = await service.IssueAsync(Email, CancellationToken.None);
        Assert.True(issued.IsSuccess);

        var verify = await service.VerifyAsync(Email, issued.Value.Code, CancellationToken.None);

        Assert.True(verify.IsSuccess);
        Assert.True(await service.IsVerifiedAsync(Email, CancellationToken.None));
    }

    [Fact]
    public async Task Verify_with_a_wrong_code_fails_and_consumes_an_attempt()
    {
        var (service, _) = Create(new OtpOptions { MaxVerifyAttempts = 2 });

        var issued = await service.IssueAsync(Email, CancellationToken.None);
        var wrongCode = new string('0', issued.Value.Code.Length) == issued.Value.Code ? "111111" : "000000";

        var first = await service.VerifyAsync(Email, wrongCode, CancellationToken.None);
        Assert.True(first.IsFailure);
        Assert.Equal(RegistrationErrors.OtpInvalid, first.Error);

        // Second wrong attempt exhausts the limit.
        var second = await service.VerifyAsync(Email, wrongCode, CancellationToken.None);
        Assert.Equal(RegistrationErrors.OtpTooManyAttempts, second.Error);
    }

    [Fact]
    public async Task Resend_within_the_cooldown_is_rejected()
    {
        var (service, clock) = Create(new OtpOptions { ResendCooldownSeconds = 60 });

        await service.IssueAsync(Email, CancellationToken.None);
        clock.Advance(TimeSpan.FromSeconds(30));

        var resend = await service.IssueAsync(Email, CancellationToken.None);

        Assert.True(resend.IsFailure);
        Assert.Equal(RegistrationErrors.OtpResendTooSoon, resend.Error);
    }

    [Fact]
    public async Task Requesting_more_than_the_per_email_cap_is_rejected()
    {
        var (service, clock) = Create(new OtpOptions { MaxRequestsPerEmail = 2, ResendCooldownSeconds = 60 });

        Assert.True((await service.IssueAsync(Email, CancellationToken.None)).IsSuccess);
        clock.Advance(TimeSpan.FromSeconds(61));
        Assert.True((await service.IssueAsync(Email, CancellationToken.None)).IsSuccess);
        clock.Advance(TimeSpan.FromSeconds(61));

        var third = await service.IssueAsync(Email, CancellationToken.None);

        Assert.True(third.IsFailure);
        Assert.Equal(RegistrationErrors.OtpRequestLimitReached, third.Error);
    }

    [Fact]
    public async Task Expired_code_cannot_be_verified()
    {
        var (service, clock) = Create(new OtpOptions { ExpiryMinutes = 10 });

        var issued = await service.IssueAsync(Email, CancellationToken.None);
        clock.Advance(TimeSpan.FromMinutes(11));

        var verify = await service.VerifyAsync(Email, issued.Value.Code, CancellationToken.None);

        Assert.True(verify.IsFailure);
        Assert.Equal(RegistrationErrors.OtpExpiredOrNotFound, verify.Error);
    }

    [Fact]
    public async Task ConsumeVerified_clears_the_verified_flag()
    {
        var (service, _) = Create();

        var issued = await service.IssueAsync(Email, CancellationToken.None);
        await service.VerifyAsync(Email, issued.Value.Code, CancellationToken.None);

        await service.ConsumeVerifiedAsync(Email, CancellationToken.None);

        Assert.False(await service.IsVerifiedAsync(Email, CancellationToken.None));
    }
}
