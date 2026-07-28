using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Registration.Application.Common.Interfaces;
using Registration.Application.Common.Models;
using Registration.Application.Common.Options;
using Registration.Domain.Common;
using Registration.Domain.Registration;
using Registration.Infrastructure.Persistence;
using Registration.Infrastructure.Security.PasswordHashing;

namespace Registration.UnitTests;

/// <summary>
/// Shared helpers for the handler tests. The repositories are the real EF implementations over the
/// InMemory provider — closer to production behaviour than hand-written fakes, and there's no mocking
/// library in this project. Only Facets is faked, since it's the one true external dependency.
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

    public static PendingRegistration NewPending(
        string email = "john.doe@gmail.com",
        string lastName = "Doe",
        DateOnly? dateOfBirth = null,
        bool withPassword = false) => new()
        {
            Id = Guid.NewGuid(),
            Email = email,
            FirstName = "John",
            LastName = lastName,
            DateOfBirth = dateOfBirth ?? new DateOnly(1990, 5, 15),
            ZipCode = "12345",
            PasswordHash = withPassword ? "hash" : null,
            PasswordSalt = withPassword ? "salt" : null,
            CreatedUtc = DateTime.UtcNow,
            ExpiresUtc = DateTime.UtcNow.AddMinutes(60)
        };
}

/// <summary>A Facets client that returns whatever the test tells it to.</summary>
internal sealed class FakeFacetsClient : IFacetsClient
{
    private readonly Result<FacetsMember> _result;

    public FakeFacetsClient(FacetsMember member) => _result = Result.Success(member);

    public FakeFacetsClient(Error error) => _result = Result.Failure<FacetsMember>(error);

    /// <summary>The lookup the handler sent — asserted on to check what gets passed to Facets.</summary>
    public FacetsMemberLookup? LastLookup { get; private set; }

    public Task<Result<FacetsMember>> FindMemberAsync(
        FacetsMemberLookup lookup,
        CancellationToken cancellationToken)
    {
        LastLookup = lookup;
        return Task.FromResult(_result);
    }

    public static FacetsMember MemberMatching(PendingRegistration pending) => new(
        SubscriberId: "SUB1234",
        MemberId: "MBR1234",
        FirstName: pending.FirstName,
        LastName: pending.LastName,
        DateOfBirth: pending.DateOfBirth,
        ZipCode: pending.ZipCode,
        Tenant: "test",
        Plan: new FacetsPlan("PLN1234", "Test Plan", "COMMERCIAL", new DateOnly(2024, 1, 1)));
}
