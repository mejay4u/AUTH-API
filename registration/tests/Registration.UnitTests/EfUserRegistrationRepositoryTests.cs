using Microsoft.EntityFrameworkCore;
using Registration.Application.Common.Models;
using Registration.Infrastructure.Persistence;
using Registration.Infrastructure.Persistence.Repositories;
using Xunit;

namespace Registration.UnitTests;

public sealed class EfUserRegistrationRepositoryTests
{
    private static RegistrationDbContext NewDb() =>
        new(new DbContextOptionsBuilder<RegistrationDbContext>()
            .UseInMemoryDatabase($"reg-{Guid.NewGuid()}")
            .Options);

    [Fact]
    public async Task CreateUser_persists_the_user_and_returns_its_id()
    {
        await using var db = NewDb();
        var repository = new EfUserRegistrationRepository(db, TimeProvider.System);

        var id = await repository.CreateUserAsync(
            new NewUserRegistration(
                "john.doe@gmail.com",
                "john.doe@gmail.com",
                "hash",
                "salt",
                "John",
                "Doe",
                new DateOnly(1990, 5, 15),
                "12345",
                "123-456-7890"),
            CancellationToken.None);

        Assert.NotEqual(Guid.Empty, id);
        Assert.True(await repository.EmailExistsAsync("john.doe@gmail.com", CancellationToken.None));
    }

    [Fact]
    public async Task EmailExists_is_false_for_an_unknown_email()
    {
        await using var db = NewDb();
        var repository = new EfUserRegistrationRepository(db, TimeProvider.System);

        Assert.False(await repository.EmailExistsAsync("nobody@gmail.com", CancellationToken.None));
    }
}
