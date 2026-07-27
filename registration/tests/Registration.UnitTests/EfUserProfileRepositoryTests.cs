using Microsoft.EntityFrameworkCore;
using Registration.Domain.Users;
using Registration.Infrastructure.Persistence;
using Registration.Infrastructure.Persistence.Repositories;
using Xunit;

namespace Registration.UnitTests;

public sealed class EfUserProfileRepositoryTests
{
    private static RegistrationDbContext NewDb() =>
        new(new DbContextOptionsBuilder<RegistrationDbContext>()
            .UseInMemoryDatabase($"profile-{Guid.NewGuid()}").Options);

    private static User NewUser(string email = "john.doe@gmail.com") => new()
    {
        Id = Guid.NewGuid(), Email = email, Username = email,
        FirstName = "John", LastName = "Doe", Origin = UserOrigin.Registration,
        IsActive = true, CreatedUtc = DateTime.UtcNow, UpdatedUtc = DateTime.UtcNow
    };

    [Fact]
    public async Task Upsert_inserts_then_GetByEmail_finds_it()
    {
        await using var db = NewDb();
        var repository = new EfUserProfileRepository(db);

        await repository.UpsertAsync(NewUser(), CancellationToken.None);

        var loaded = await repository.GetByEmailAsync("john.doe@gmail.com", CancellationToken.None);
        Assert.NotNull(loaded);
        Assert.Equal("Doe", loaded!.LastName);
    }

    [Fact]
    public async Task Upsert_updates_a_tracked_entity_in_place()
    {
        await using var db = NewDb();
        var repository = new EfUserProfileRepository(db);
        await repository.UpsertAsync(NewUser(), CancellationToken.None);

        var loaded = await repository.GetByEmailAsync("john.doe@gmail.com", CancellationToken.None);
        loaded!.LastName = "Smith";
        await repository.UpsertAsync(loaded, CancellationToken.None);

        Assert.Equal(1, await db.Users.CountAsync());
        Assert.Equal("Smith", (await db.Users.SingleAsync()).LastName);
    }
}
