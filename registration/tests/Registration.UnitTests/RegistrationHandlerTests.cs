using Microsoft.Extensions.Logging.Abstractions;
using Registration.Application.Common.Models;
using Registration.Application.Registration.CreateAccount;
using Registration.Application.Registration.InitiateRegistration;
using Registration.Domain.Registration;
using Registration.Infrastructure.Persistence;
using Registration.Infrastructure.Persistence.Repositories;
using Xunit;

namespace Registration.UnitTests;

public sealed class InitiateRegistrationCommandHandlerTests
{
    private static InitiateRegistrationCommand NewCommand(string email = "john.doe@gmail.com") =>
        new(email, "John", "Doe", new DateOnly(1990, 5, 15), "12345", "123-456-7890");

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

        var result = await NewHandler(db).Handle(NewCommand(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(InitiateRegistrationResult.PendingStatus, result.Value.Status);
        Assert.NotEqual(Guid.Empty, result.Value.UserId);

        var stored = db.PendingRegistrations.Single();
        Assert.Equal("john.doe@gmail.com", stored.Email);
        Assert.Equal("123-456-7890", stored.ContactNumber);
    }

    [Fact]
    public async Task Normalizes_the_email_before_storing_it()
    {
        await using var db = TestDoubles.NewDb();

        var result = await NewHandler(db)
            .Handle(NewCommand("  John.Doe@Gmail.COM "), CancellationToken.None);

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
        Assert.Single(db.PendingRegistrations);
    }

    [Fact]
    public async Task Replaces_an_expired_record_rather_than_colliding_with_it()
    {
        await using var db = TestDoubles.NewDb();
        var clock = new TestTimeProvider();
        var handler = NewHandler(db, clock);

        var first = await handler.Handle(NewCommand(), CancellationToken.None);

        clock.Advance(TimeSpan.FromMinutes(61)); // past the 60-minute lifetime

        var second = await handler.Handle(NewCommand(), CancellationToken.None);

        Assert.True(second.IsSuccess);
        Assert.NotEqual(first.Value.UserId, second.Value.UserId);
        Assert.Single(db.PendingRegistrations);
    }

    [Fact]
    public async Task Rejects_an_email_that_already_has_an_account()
    {
        await using var db = TestDoubles.NewDb();
        await new EfUserRegistrationRepository(db, TimeProvider.System).CreateUserAsync(
            new NewUserRegistration(
                Guid.NewGuid(), "john.doe@gmail.com", "john.doe@gmail.com", "hash", "salt",
                "John", "Doe", new DateOnly(1990, 5, 15), "12345", null),
            CancellationToken.None);

        var result = await NewHandler(db).Handle(NewCommand(), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(RegistrationErrors.EmailAlreadyRegistered, result.Error);
    }
}

public sealed class CreateAccountCommandHandlerTests
{
    private const string Password = "Str0ng!PassPhrase";

    private static CreateAccountCommandHandler NewHandler(
        RegistrationDbContext db,
        TimeProvider? timeProvider = null) =>
        new(new EfPendingRegistrationRepository(db),
            new EfUserRegistrationRepository(db, timeProvider ?? TimeProvider.System),
            TestDoubles.NewHasher(),
            timeProvider ?? TimeProvider.System,
            NullLogger<CreateAccountCommandHandler>.Instance);

    private static async Task<PendingRegistration> SeedAsync(RegistrationDbContext db)
    {
        var pending = TestDoubles.NewPending();
        await new EfPendingRegistrationRepository(db).CreateAsync(pending, CancellationToken.None);
        return pending;
    }

    [Fact]
    public async Task Creates_the_user_from_the_pending_record()
    {
        await using var db = TestDoubles.NewDb();
        var pending = await SeedAsync(db);

        var result = await NewHandler(db)
            .Handle(new CreateAccountCommand(pending.Id, Password, Password), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(pending.Email, result.Value.Email);
        Assert.Equal(pending.Email, result.Value.Username);

        var user = db.Users.Single();
        Assert.Equal("John", user.FirstName);
        Assert.Equal("123-456-7890", user.ContactNumber);
        Assert.True(user.IsActive);
    }

    [Fact]
    public async Task Keeps_the_pending_records_id_so_the_apps_identifier_stays_valid()
    {
        await using var db = TestDoubles.NewDb();
        var pending = await SeedAsync(db);

        var result = await NewHandler(db)
            .Handle(new CreateAccountCommand(pending.Id, Password, Password), CancellationToken.None);

        Assert.Equal(pending.Id, result.Value.UserId);
    }

    [Fact]
    public async Task Stores_a_hash_rather_than_the_password()
    {
        await using var db = TestDoubles.NewDb();
        var pending = await SeedAsync(db);

        await NewHandler(db)
            .Handle(new CreateAccountCommand(pending.Id, Password, Password), CancellationToken.None);

        var user = db.Users.Single();
        Assert.NotEqual(Password, user.PasswordHash);
        Assert.False(string.IsNullOrEmpty(user.PasswordSalt));
        Assert.DoesNotContain(Password, System.Text.Json.JsonSerializer.Serialize(user));
    }

    [Fact]
    public async Task Deletes_the_pending_record_once_promoted()
    {
        await using var db = TestDoubles.NewDb();
        var pending = await SeedAsync(db);

        await NewHandler(db)
            .Handle(new CreateAccountCommand(pending.Id, Password, Password), CancellationToken.None);

        Assert.Empty(db.PendingRegistrations);
    }

    [Fact]
    public async Task Refuses_an_unknown_registration()
    {
        await using var db = TestDoubles.NewDb();

        var result = await NewHandler(db)
            .Handle(new CreateAccountCommand(Guid.NewGuid(), Password, Password), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(RegistrationErrors.RegistrationNotFoundOrExpired, result.Error);
        Assert.Empty(db.Users);
    }

    [Fact]
    public async Task Refuses_an_expired_registration()
    {
        await using var db = TestDoubles.NewDb();
        var clock = new TestTimeProvider();
        var pending = TestDoubles.NewPending();
        pending.ExpiresUtc = clock.GetUtcNow().UtcDateTime.AddMinutes(-1);
        await new EfPendingRegistrationRepository(db).CreateAsync(pending, CancellationToken.None);

        var result = await NewHandler(db, clock)
            .Handle(new CreateAccountCommand(pending.Id, Password, Password), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(RegistrationErrors.RegistrationNotFoundOrExpired, result.Error);
        Assert.Empty(db.Users);
    }

    [Fact]
    public async Task Refuses_when_the_email_was_registered_in_the_meantime()
    {
        await using var db = TestDoubles.NewDb();
        var pending = await SeedAsync(db);
        await new EfUserRegistrationRepository(db, TimeProvider.System).CreateUserAsync(
            new NewUserRegistration(
                Guid.NewGuid(), pending.Email, pending.Email, "hash", "salt",
                "John", "Doe", new DateOnly(1990, 5, 15), "12345", null),
            CancellationToken.None);

        var result = await NewHandler(db)
            .Handle(new CreateAccountCommand(pending.Id, Password, Password), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(RegistrationErrors.EmailAlreadyRegistered, result.Error);
    }
}
