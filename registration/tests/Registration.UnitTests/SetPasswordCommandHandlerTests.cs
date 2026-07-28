using Microsoft.Extensions.Logging.Abstractions;
using Registration.Application.Registration.SetPassword;
using Registration.Domain.Registration;
using Registration.Infrastructure.Persistence;
using Registration.Infrastructure.Persistence.Repositories;
using Xunit;

namespace Registration.UnitTests;

public sealed class SetPasswordCommandHandlerTests
{
    private const string Password = "Str0ng!Pass";

    private static SetPasswordCommandHandler NewHandler(
        RegistrationDbContext db,
        TimeProvider? timeProvider = null) =>
        new(new EfPendingRegistrationRepository(db),
            TestDoubles.NewHasher(),
            timeProvider ?? TimeProvider.System,
            NullLogger<SetPasswordCommandHandler>.Instance);

    [Fact]
    public async Task Stores_a_hash_and_salt_rather_than_the_password()
    {
        await using var db = TestDoubles.NewDb();
        var pending = TestDoubles.NewPending();
        await new EfPendingRegistrationRepository(db).CreateAsync(pending, CancellationToken.None);

        var result = await NewHandler(db)
            .Handle(new SetPasswordCommand(pending.Id, Password, Password), CancellationToken.None);

        Assert.True(result.IsSuccess);

        var stored = db.PendingRegistrations.Single();
        Assert.True(stored.HasPassword);
        Assert.NotEqual(Password, stored.PasswordHash);
        Assert.False(string.IsNullOrEmpty(stored.PasswordSalt));
    }

    [Fact]
    public async Task Replaces_an_earlier_password_when_run_again()
    {
        await using var db = TestDoubles.NewDb();
        var pending = TestDoubles.NewPending();
        await new EfPendingRegistrationRepository(db).CreateAsync(pending, CancellationToken.None);
        var handler = NewHandler(db);

        await handler.Handle(new SetPasswordCommand(pending.Id, Password, Password), CancellationToken.None);
        var first = db.PendingRegistrations.Single().PasswordHash;

        await handler.Handle(
            new SetPasswordCommand(pending.Id, "An0ther!Pass", "An0ther!Pass"), CancellationToken.None);
        var second = db.PendingRegistrations.Single().PasswordHash;

        Assert.NotEqual(first, second);
    }

    [Fact]
    public async Task Refuses_an_unknown_registration()
    {
        await using var db = TestDoubles.NewDb();

        var result = await NewHandler(db)
            .Handle(new SetPasswordCommand(Guid.NewGuid(), Password, Password), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(RegistrationErrors.RegistrationNotFoundOrExpired, result.Error);
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
            .Handle(new SetPasswordCommand(pending.Id, Password, Password), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(RegistrationErrors.RegistrationNotFoundOrExpired, result.Error);
    }
}
