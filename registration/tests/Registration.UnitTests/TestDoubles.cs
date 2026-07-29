using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Registration.Application.Common.Interfaces;
using Registration.Application.Common.Options;
using Registration.Domain.Registration;
using Registration.Infrastructure.Persistence;
using Registration.Infrastructure.Security.PasswordHashing;

namespace Registration.UnitTests;

/// <summary>
/// Shared helpers for the handler tests. The repositories are the real EF implementations over the
/// InMemory provider — closer to production behaviour than hand-written fakes, and there's no mocking
/// library in this project.
/// </summary>
internal static class TestDoubles
{
    public static RegistrationDbContext NewDb(string prefix = "reg") =>
        new(new DbContextOptionsBuilder<RegistrationDbContext>()
            .UseInMemoryDatabase($"{prefix}-{Guid.NewGuid()}")
            .Options);

    public static IPasswordHasher NewHasher() =>
        new Pbkdf2PasswordHasher(Options.Create(new PasswordHashingOptions
        {
            // Deliberately weak: these tests hash on every run and the work factor isn't what's
            // under test (Pbkdf2PasswordHasherTests covers the real parameters).
            Iterations = 1,
            SaltSizeBytes = 16,
            HashSizeBytes = 32
        }));

    public static IOptions<RegistrationOptions> NewRegistrationOptions(int sessionLifetimeMinutes = 60) =>
        Options.Create(new RegistrationOptions { SessionLifetimeMinutes = sessionLifetimeMinutes });

    public static IOptions<PasswordPolicyOptions> NewPasswordPolicy() =>
        Options.Create(new PasswordPolicyOptions());

    public static PendingRegistration NewPending(string email = "john.doe@gmail.com") => new()
    {
        Id = Guid.NewGuid(),
        Email = email,
        DescopeUserId = "descope-user-1",
        FirstName = "John",
        LastName = "Doe",
        DateOfBirth = new DateOnly(1990, 5, 15),
        ZipCode = "12345",
        ContactNumber = "123-456-7890",
        CreatedUtc = DateTime.UtcNow,
        ExpiresUtc = DateTime.UtcNow.AddMinutes(60)
    };
}
