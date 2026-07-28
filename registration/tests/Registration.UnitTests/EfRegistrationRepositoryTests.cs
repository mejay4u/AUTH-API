using Registration.Application.Common.Models;
using Registration.Infrastructure.Persistence.Repositories;
using Xunit;

namespace Registration.UnitTests;

public sealed class EfPendingRegistrationRepositoryTests
{
    [Fact]
    public async Task Create_then_Get_round_trips_the_record()
    {
        await using var db = TestDoubles.NewDb("pending");
        var repository = new EfPendingRegistrationRepository(db);
        var pending = TestDoubles.NewPending();

        await repository.CreateAsync(pending, CancellationToken.None);
        var loaded = await repository.GetAsync(pending.Id, CancellationToken.None);

        Assert.NotNull(loaded);
        Assert.Equal(pending.Email, loaded!.Email);
        Assert.Equal(pending.FirstName, loaded.FirstName);
        Assert.Equal(pending.DateOfBirth, loaded.DateOfBirth);
        Assert.False(loaded.HasPassword);
    }

    [Fact]
    public async Task GetByEmail_finds_the_record()
    {
        await using var db = TestDoubles.NewDb("pending");
        var repository = new EfPendingRegistrationRepository(db);
        var pending = TestDoubles.NewPending();

        await repository.CreateAsync(pending, CancellationToken.None);
        var loaded = await repository.GetByEmailAsync(pending.Email, CancellationToken.None);

        Assert.NotNull(loaded);
        Assert.Equal(pending.Id, loaded!.Id);
    }

    [Fact]
    public async Task GetByEmail_returns_null_for_an_unknown_address()
    {
        await using var db = TestDoubles.NewDb("pending");
        var repository = new EfPendingRegistrationRepository(db);

        Assert.Null(await repository.GetByEmailAsync("nobody@example.com", CancellationToken.None));
    }

    [Fact]
    public async Task SetPassword_stores_both_hash_and_salt()
    {
        await using var db = TestDoubles.NewDb("pending");
        var repository = new EfPendingRegistrationRepository(db);
        var pending = TestDoubles.NewPending();
        await repository.CreateAsync(pending, CancellationToken.None);

        await repository.SetPasswordAsync(pending.Id, "the-hash", "the-salt", CancellationToken.None);
        var loaded = await repository.GetAsync(pending.Id, CancellationToken.None);

        Assert.True(loaded!.HasPassword);
        Assert.Equal("the-hash", loaded.PasswordHash);
        Assert.Equal("the-salt", loaded.PasswordSalt);
    }

    [Fact]
    public async Task SetPassword_on_an_unknown_id_is_a_no_op()
    {
        await using var db = TestDoubles.NewDb("pending");
        var repository = new EfPendingRegistrationRepository(db);

        await repository.SetPasswordAsync(Guid.NewGuid(), "h", "s", CancellationToken.None);

        Assert.Empty(db.PendingRegistrations);
    }

    [Fact]
    public async Task Delete_removes_the_record()
    {
        await using var db = TestDoubles.NewDb("pending");
        var repository = new EfPendingRegistrationRepository(db);
        var pending = TestDoubles.NewPending();
        await repository.CreateAsync(pending, CancellationToken.None);

        await repository.DeleteAsync(pending.Id, CancellationToken.None);

        Assert.Null(await repository.GetAsync(pending.Id, CancellationToken.None));
    }
}

public sealed class EfUserRegistrationRepositoryTests
{
    private static NewUserRegistration NewUser(Guid id, string email = "john.doe@gmail.com") =>
        new(id, email, email, "hash", "salt", "John", "Doe",
            new DateOnly(1990, 5, 15), "12345", "SUB1234", "PLN1234", "6789");

    [Fact]
    public async Task CreateUser_keeps_the_id_it_was_given()
    {
        await using var db = TestDoubles.NewDb();
        var repository = new EfUserRegistrationRepository(db, TimeProvider.System);
        var id = Guid.NewGuid();

        var created = await repository.CreateUserAsync(NewUser(id), CancellationToken.None);

        // The pending record's id carries over, so the identifier Descope holds stays valid.
        Assert.Equal(id, created);
    }

    [Fact]
    public async Task CreateUser_persists_the_eligibility_it_was_given()
    {
        await using var db = TestDoubles.NewDb();
        var repository = new EfUserRegistrationRepository(db, TimeProvider.System);

        await repository.CreateUserAsync(NewUser(Guid.NewGuid()), CancellationToken.None);

        var user = db.Users.Single();
        Assert.Equal("SUB1234", user.SubscriberId);
        Assert.Equal("PLN1234", user.PlanId);
        Assert.Equal("6789", user.SsnLast4);
        Assert.True(user.IsActive);
    }

    [Fact]
    public async Task EmailExists_is_true_only_after_the_user_is_created()
    {
        await using var db = TestDoubles.NewDb();
        var repository = new EfUserRegistrationRepository(db, TimeProvider.System);

        Assert.False(await repository.EmailExistsAsync("john.doe@gmail.com", CancellationToken.None));

        await repository.CreateUserAsync(NewUser(Guid.NewGuid()), CancellationToken.None);

        Assert.True(await repository.EmailExistsAsync("john.doe@gmail.com", CancellationToken.None));
    }
}
