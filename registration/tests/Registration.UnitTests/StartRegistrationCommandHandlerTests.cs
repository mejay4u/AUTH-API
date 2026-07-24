using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Registration.Application.Common.Interfaces;
using Registration.Application.Common.Models;
using Registration.Application.Common.Options;
using Registration.Application.Registration.StartRegistration;
using Registration.Domain.Common;
using Registration.Domain.Registration;
using Xunit;

namespace Registration.UnitTests;

public sealed class StartRegistrationCommandHandlerTests
{
    private static StartRegistrationCommand Command(string email = "john.doe@gmail.com") =>
        new("John", "Doe", new DateOnly(1990, 5, 15), "12345", email, "123-456-7890");

    private static StartRegistrationCommandHandler CreateHandler(
        FakePendingRepository pending, FakeUserRepository users, RecordingOtpService otp, RecordingEmailSender email) =>
        new(pending, users, otp, email, Options.Create(new RegistrationOptions()), TimeProvider.System,
            NullLogger<StartRegistrationCommandHandler>.Instance);

    [Fact]
    public async Task Creates_a_session_and_issues_a_code_for_a_new_email()
    {
        var pending = new FakePendingRepository();
        var otp = new RecordingOtpService();
        var email = new RecordingEmailSender();
        var handler = CreateHandler(pending, new FakeUserRepository(exists: false), otp, email);

        var result = await handler.Handle(Command(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotEqual(Guid.Empty, result.Value.RegistrationId);
        Assert.Equal(1, pending.CreateCalls);
        Assert.Equal(1, otp.IssueCalls);
        Assert.Equal(1, email.SendCalls);
    }

    [Fact]
    public async Task Creates_a_session_but_issues_no_code_for_an_already_registered_email()
    {
        var pending = new FakePendingRepository();
        var otp = new RecordingOtpService();
        var email = new RecordingEmailSender();
        var handler = CreateHandler(pending, new FakeUserRepository(exists: true), otp, email);

        var result = await handler.Handle(Command(), CancellationToken.None);

        // Enumeration-safe: a session id is still returned, but no code is issued or sent.
        Assert.True(result.IsSuccess);
        Assert.NotEqual(Guid.Empty, result.Value.RegistrationId);
        Assert.Equal(1, pending.CreateCalls);
        Assert.Equal(0, otp.IssueCalls);
        Assert.Equal(0, email.SendCalls);
    }

    private sealed class FakePendingRepository : IPendingRegistrationRepository
    {
        public int CreateCalls { get; private set; }

        public Task<Guid> CreateAsync(PendingRegistration pending, CancellationToken cancellationToken)
        {
            CreateCalls++;
            return Task.FromResult(pending.Id);
        }

        public Task<PendingRegistration?> GetAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult<PendingRegistration?>(null);

        public Task MarkEmailVerifiedAsync(Guid id, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task DeleteAsync(Guid id, CancellationToken cancellationToken) => Task.CompletedTask;
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
