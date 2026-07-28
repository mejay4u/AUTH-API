using Microsoft.Extensions.Logging.Abstractions;
using Registration.Application.Common.Models;
using Registration.Application.Registration.InitiateRegistration;
using Registration.Domain.Registration;
using Registration.Infrastructure.Persistence;
using Registration.Infrastructure.Persistence.Repositories;
using Xunit;

namespace Registration.UnitTests;

public sealed class InitiateRegistrationCommandHandlerTests
{
    private static InitiateRegistrationCommand NewCommand(string email = "john.doe@gmail.com") =>
        new(email, "John", "Doe", new DateOnly(1990, 5, 15), "12345");

    private static InitiateRegistrationCommandHandler NewHandler(
        RegistrationDbContext db,
        TimeProvider? timeProvider = null) =>
        new(new EfPendingRegistrationRepository(db),
            new EfUserRegistrationRepository(db, timeProvider ?? TimeProvider.System),
            TestDoubles.NewRegistrationOptions(),
            timeProvider ?? TimeProvider.System,
            NullLogger<InitiateRegistrationCommandHandler>.Instance);

    [Fact]
    public async Task Creates_a_pending_record_and_returns_its_id()
    {
        await using var db = TestDoubles.NewDb();
        var handler = NewHandler(db);

        var result = await handler.Handle(NewCommand(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(InitiateRegistrationResult.PendingStatus, result.Value.Status);
        Assert.NotEqual(Guid.Empty, result.Value.UserId);

        var stored = await new EfPendingRegistrationRepository(db)
            .GetAsync(result.Value.UserId, CancellationToken.None);
        Assert.NotNull(stored);
        Assert.Equal("john.doe@gmail.com", stored!.Email);
        // Nothing has set a password yet — completeRegistration must refuse until it does.
        Assert.False(stored.HasPassword);
    }

    [Fact]
    public async Task Normalizes_the_email_before_storing_it()
    {
        await using var db = TestDoubles.NewDb();
        var handler = NewHandler(db);

        var result = await handler.Handle(NewCommand("  John.Doe@Gmail.COM "), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("john.doe@gmail.com", result.Value.Email);
    }

    [Fact]
    public async Task Reuses_the_existing_record_when_the_member_starts_again()
    {
        await using var db = TestDoubles.NewDb();
        var handler = NewHandler(db);

        var first = await handler.Handle(NewCommand(), CancellationToken.None);
        var second = await handler.Handle(NewCommand(), CancellationToken.None);

        Assert.True(second.IsSuccess);
        Assert.Equal(first.Value.UserId, second.Value.UserId);
    }

    [Fact]
    public async Task Replaces_an_expired_record_rather_than_colliding_with_it()
    {
        await using var db = TestDoubles.NewDb();
        var clock = new TestTimeProvider();
        var handler = NewHandler(db, clock);

        var first = await handler.Handle(NewCommand(), CancellationToken.None);

        // Past the 60-minute session lifetime.
        clock.Advance(TimeSpan.FromMinutes(61));

        var second = await handler.Handle(NewCommand(), CancellationToken.None);

        Assert.True(second.IsSuccess);
        Assert.NotEqual(first.Value.UserId, second.Value.UserId);
    }

    [Fact]
    public async Task Rejects_an_email_that_already_has_an_account()
    {
        await using var db = TestDoubles.NewDb();
        await new EfUserRegistrationRepository(db, TimeProvider.System).CreateUserAsync(
            new NewUserRegistration(
                Guid.NewGuid(), "john.doe@gmail.com", "john.doe@gmail.com", "hash", "salt",
                "John", "Doe", new DateOnly(1990, 5, 15), "12345", "SUB1", "PLN1", "6789"),
            CancellationToken.None);

        var result = await NewHandler(db).Handle(NewCommand(), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(RegistrationErrors.EmailAlreadyRegistered, result.Error);
    }
}
