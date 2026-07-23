using Microsoft.Extensions.Logging.Abstractions;
using Registration.Application.Common.Interfaces;
using Registration.Application.Common.Models;
using Registration.Application.Registration.SendEmailOtp;
using Registration.Domain.Common;
using Xunit;

namespace Registration.UnitTests;

public sealed class SendEmailOtpCommandHandlerTests
{
    [Fact]
    public async Task Does_not_issue_or_send_a_code_when_the_email_is_already_registered()
    {
        var otp = new RecordingOtpService();
        var email = new RecordingEmailSender();
        var handler = new SendEmailOtpCommandHandler(
            new FakeUserRepository(exists: true), otp, email, NullLogger<SendEmailOtpCommandHandler>.Instance);

        var result = await handler.Handle(new SendEmailOtpCommand("john.doe@gmail.com"), CancellationToken.None);

        // Enumeration-safe: same generic success, but nothing issued or emailed.
        Assert.True(result.IsSuccess);
        Assert.Equal(0, otp.IssueCalls);
        Assert.Equal(0, email.SendCalls);
    }

    [Fact]
    public async Task Issues_and_sends_a_code_for_a_new_email()
    {
        var otp = new RecordingOtpService();
        var email = new RecordingEmailSender();
        var handler = new SendEmailOtpCommandHandler(
            new FakeUserRepository(exists: false), otp, email, NullLogger<SendEmailOtpCommandHandler>.Instance);

        var result = await handler.Handle(new SendEmailOtpCommand("jane.roe@gmail.com"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, otp.IssueCalls);
        Assert.Equal(1, email.SendCalls);
    }

    private sealed class FakeUserRepository(bool exists) : IUserRegistrationRepository
    {
        public Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken) =>
            Task.FromResult(exists);

        public Task<Guid> CreateUserAsync(NewUserRegistration registration, CancellationToken cancellationToken) =>
            Task.FromResult(Guid.NewGuid());
    }

    private sealed class RecordingOtpService : IOtpService
    {
        public int IssueCalls { get; private set; }

        public Task<Result<OtpChallenge>> IssueAsync(string email, CancellationToken cancellationToken)
        {
            IssueCalls++;
            return Task.FromResult(Result.Success(new OtpChallenge("123456", DateTimeOffset.UtcNow.AddMinutes(10))));
        }

        public Task<Result> VerifyAsync(string email, string code, CancellationToken cancellationToken) =>
            Task.FromResult(Result.Success());

        public Task<bool> IsVerifiedAsync(string email, CancellationToken cancellationToken) =>
            Task.FromResult(true);

        public Task ConsumeVerifiedAsync(string email, CancellationToken cancellationToken) =>
            Task.CompletedTask;
    }

    private sealed class RecordingEmailSender : IEmailSender
    {
        public int SendCalls { get; private set; }

        public Task SendOtpAsync(string email, string code, DateTimeOffset expiresUtc, CancellationToken cancellationToken)
        {
            SendCalls++;
            return Task.CompletedTask;
        }
    }
}
