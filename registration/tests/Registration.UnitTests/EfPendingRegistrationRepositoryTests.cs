using Microsoft.EntityFrameworkCore;
using Registration.Domain.Registration;
using Registration.Infrastructure.Persistence;
using Registration.Infrastructure.Persistence.Repositories;
using Xunit;

namespace Registration.UnitTests;

public sealed class EfPendingRegistrationRepositoryTests
{
    private static RegistrationDbContext NewDb() =>
        new(new DbContextOptionsBuilder<RegistrationDbContext>()
            .UseInMemoryDatabase($"pending-{Guid.NewGuid()}")
            .Options);

    private static PendingRegistration NewSession() => new()
    {
        Id = Guid.NewGuid(),
        Email = "john.doe@gmail.com",
        FirstName = "John",
        LastName = "Doe",
        DateOfBirth = new DateOnly(1990, 5, 15),
        ZipCode = "12345",
        ContactNumber = "123-456-7890",
        EmailVerified = false,
        CreatedUtc = DateTime.UtcNow,
        ExpiresUtc = DateTime.UtcNow.AddMinutes(60)
    };

    [Fact]
    public async Task Create_then_Get_round_trips_the_session()
    {
        await using var db = NewDb();
        var repository = new EfPendingRegistrationRepository(db);
        var session = NewSession();

        await repository.CreateAsync(session, CancellationToken.None);
        var loaded = await repository.GetAsync(session.Id, CancellationToken.None);

        Assert.NotNull(loaded);
        Assert.Equal("john.doe@gmail.com", loaded!.Email);
        Assert.False(loaded.EmailVerified);
    }

    [Fact]
    public async Task MarkEmailVerified_sets_the_flag()
    {
        await using var db = NewDb();
        var repository = new EfPendingRegistrationRepository(db);
        var session = NewSession();
        await repository.CreateAsync(session, CancellationToken.None);

        await repository.MarkEmailVerifiedAsync(session.Id, CancellationToken.None);
        var loaded = await repository.GetAsync(session.Id, CancellationToken.None);

        Assert.True(loaded!.EmailVerified);
    }

    [Fact]
    public async Task Delete_removes_the_session()
    {
        await using var db = NewDb();
        var repository = new EfPendingRegistrationRepository(db);
        var session = NewSession();
        await repository.CreateAsync(session, CancellationToken.None);

        await repository.DeleteAsync(session.Id, CancellationToken.None);
        var loaded = await repository.GetAsync(session.Id, CancellationToken.None);

        Assert.Null(loaded);
    }
}
