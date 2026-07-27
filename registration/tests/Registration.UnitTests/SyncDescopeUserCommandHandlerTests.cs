using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Registration.Application.Registration.SyncDescopeUser;
using Registration.Infrastructure.Persistence;
using Registration.Infrastructure.Persistence.Repositories;
using Xunit;

namespace Registration.UnitTests;

public sealed class SyncDescopeUserCommandHandlerTests
{
    private static RegistrationDbContext NewDb() =>
        new(new DbContextOptionsBuilder<RegistrationDbContext>()
            .UseInMemoryDatabase($"sync-{Guid.NewGuid()}").Options);

    private static SyncDescopeUserCommand Command(string email = "jane.roe@gmail.com", string descopeId = "D-1") =>
        new(descopeId, email, "Jane", "Roe", new DateOnly(1990, 1, 1), "12345", null);

    private static SyncDescopeUserCommandHandler Handler(RegistrationDbContext db) =>
        new(new EfUserProfileRepository(db), TimeProvider.System, NullLogger<SyncDescopeUserCommandHandler>.Instance);

    [Fact]
    public async Task Inserts_a_new_user_with_Registration_origin()
    {
        await using var db = NewDb();

        var result = await Handler(db).Handle(Command(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var user = await db.Users.SingleAsync();
        Assert.Equal("Registration", user.Origin);
        Assert.Equal("D-1", user.DescopeUserId);
    }

    [Fact]
    public async Task Is_idempotent_for_the_same_email()
    {
        await using var db = NewDb();
        var handler = Handler(db);

        await handler.Handle(Command(email: "jane.roe@gmail.com"), CancellationToken.None);
        await handler.Handle(Command(email: "jane.roe@gmail.com"), CancellationToken.None);

        Assert.Equal(1, await db.Users.CountAsync());
    }
}
