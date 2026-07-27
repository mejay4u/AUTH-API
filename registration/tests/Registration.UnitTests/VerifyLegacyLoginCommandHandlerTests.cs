using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Registration.Application.Common.Interfaces;
using Registration.Application.Common.Models;
using Registration.Application.Registration.VerifyLegacyLogin;
using Registration.Domain.Registration;
using Registration.Domain.Users;
using Registration.Infrastructure.Persistence;
using Registration.Infrastructure.Persistence.Repositories;
using Xunit;

namespace Registration.UnitTests;

public sealed class VerifyLegacyLoginCommandHandlerTests
{
    private static RegistrationDbContext NewDb() =>
        new(new DbContextOptionsBuilder<RegistrationDbContext>()
            .UseInMemoryDatabase($"jit-{Guid.NewGuid()}").Options);

    private static VerifyLegacyLoginCommandHandler Handler(RegistrationDbContext db, ILegacyMemberVerifier verifier) =>
        new(new EfUserProfileRepository(db), new EfMigrationAuditRepository(db), verifier,
            TimeProvider.System, NullLogger<VerifyLegacyLoginCommandHandler>.Instance);

    [Fact]
    public async Task Returns_profile_without_calling_legacy_when_user_already_exists()
    {
        await using var db = NewDb();
        db.Users.Add(new User
        {
            Id = Guid.NewGuid(), Email = "john.doe@x.com", Username = "john.doe@x.com",
            FirstName = "John", LastName = "Doe", Origin = UserOrigin.Registration,
            IsActive = true, CreatedUtc = DateTime.UtcNow, UpdatedUtc = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var verifier = new FakeVerifier(LegacyVerificationResult.Failed);
        var result = await Handler(db, verifier).Handle(
            new VerifyLegacyLoginCommand("john.doe@x.com", "whatever", null), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(0, verifier.Calls); // legacy system not touched
    }

    [Fact]
    public async Task Migrates_and_audits_on_valid_legacy_credentials()
    {
        await using var db = NewDb();
        var profile = new LegacyMemberProfile("new.member@x.com", "New", "Member", "LEG-9",
            new DateOnly(1980, 2, 2), "54321", null);

        var result = await Handler(db, new FakeVerifier(LegacyVerificationResult.Success(profile)))
            .Handle(new VerifyLegacyLoginCommand("new.member@x.com", "pw", "1.2.3.4"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(UserOrigin.Migrated, result.Value.Origin);

        var user = await db.Users.SingleAsync();
        Assert.Equal(UserOrigin.Migrated, user.Origin);
        Assert.Equal("LEG-9", user.LegacyMemberId);
        Assert.NotNull(user.MigratedUtc);
        Assert.True(await db.MigrationAudits.AnyAsync(a => a.Success));
    }

    [Fact]
    public async Task Fails_and_audits_on_invalid_legacy_credentials()
    {
        await using var db = NewDb();

        var result = await Handler(db, new FakeVerifier(LegacyVerificationResult.Failed))
            .Handle(new VerifyLegacyLoginCommand("nobody@x.com", "pw", null), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(RegistrationErrors.LegacyVerificationFailed, result.Error);
        Assert.Equal(0, await db.Users.CountAsync());
        Assert.True(await db.MigrationAudits.AnyAsync(a => !a.Success));
    }

    private sealed class FakeVerifier(LegacyVerificationResult result) : ILegacyMemberVerifier
    {
        public int Calls { get; private set; }

        public Task<LegacyVerificationResult> VerifyAsync(string email, string password, CancellationToken cancellationToken)
        {
            Calls++;
            return Task.FromResult(result);
        }
    }
}
