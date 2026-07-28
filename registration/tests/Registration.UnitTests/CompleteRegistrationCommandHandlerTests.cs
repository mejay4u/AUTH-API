using Microsoft.Extensions.Logging.Abstractions;
using Registration.Application.Registration.CompleteRegistration;
using Registration.Domain.Registration;
using Registration.Infrastructure.Persistence;
using Registration.Infrastructure.Persistence.Repositories;
using Xunit;

namespace Registration.UnitTests;

public sealed class CompleteRegistrationCommandHandlerTests
{
    private const string Ssn = "123-45-6789";

    private static CompleteRegistrationCommandHandler NewHandler(
        RegistrationDbContext db,
        FakeFacetsClient facets) =>
        new(new EfPendingRegistrationRepository(db),
            new EfUserRegistrationRepository(db, TimeProvider.System),
            facets,
            TimeProvider.System,
            NullLogger<CompleteRegistrationCommandHandler>.Instance);

    private static async Task<PendingRegistration> SeedPendingAsync(
        RegistrationDbContext db,
        bool withPassword = true,
        string lastName = "Doe")
    {
        var pending = TestDoubles.NewPending(withPassword: withPassword, lastName: lastName);
        await new EfPendingRegistrationRepository(db).CreateAsync(pending, CancellationToken.None);
        return pending;
    }

    [Fact]
    public async Task Creates_the_user_and_returns_subscriber_and_plan()
    {
        await using var db = TestDoubles.NewDb();
        var pending = await SeedPendingAsync(db);
        var facets = new FakeFacetsClient(FakeFacetsClient.MemberMatching(pending));

        var result = await NewHandler(db, facets)
            .Handle(new CompleteRegistrationCommand(pending.Email, Ssn, null), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value.Complete);
        Assert.Equal("SUB1234", result.Value.MemberInfo.SubscriberId);
        Assert.Equal("PLN1234", result.Value.PlanInfo.PlanId);

        // The id survives the promotion, so the identifier Descope was handed still resolves.
        Assert.Equal(pending.Id, result.Value.UserId);
    }

    [Fact]
    public async Task Stores_only_the_last_four_digits_of_the_ssn()
    {
        await using var db = TestDoubles.NewDb();
        var pending = await SeedPendingAsync(db);
        var facets = new FakeFacetsClient(FakeFacetsClient.MemberMatching(pending));

        await NewHandler(db, facets)
            .Handle(new CompleteRegistrationCommand(pending.Email, Ssn, null), CancellationToken.None);

        var user = db.Users.Single();
        Assert.Equal("6789", user.SsnLast4);
        // Nothing anywhere on the record may contain the full number.
        Assert.DoesNotContain("123456789", System.Text.Json.JsonSerializer.Serialize(user));
    }

    [Fact]
    public async Task Sends_the_ssn_to_facets_without_formatting()
    {
        await using var db = TestDoubles.NewDb();
        var pending = await SeedPendingAsync(db);
        var facets = new FakeFacetsClient(FakeFacetsClient.MemberMatching(pending));

        await NewHandler(db, facets)
            .Handle(new CompleteRegistrationCommand(pending.Email, Ssn, null), CancellationToken.None);

        Assert.Equal("123456789", facets.LastLookup!.Ssn);
    }

    [Fact]
    public async Task Deletes_the_pending_record_once_promoted()
    {
        await using var db = TestDoubles.NewDb();
        var pending = await SeedPendingAsync(db);
        var facets = new FakeFacetsClient(FakeFacetsClient.MemberMatching(pending));

        await NewHandler(db, facets)
            .Handle(new CompleteRegistrationCommand(pending.Email, Ssn, null), CancellationToken.None);

        Assert.Empty(db.PendingRegistrations);
    }

    [Fact]
    public async Task Refuses_when_no_password_has_been_set()
    {
        await using var db = TestDoubles.NewDb();
        var pending = await SeedPendingAsync(db, withPassword: false);
        var facets = new FakeFacetsClient(FakeFacetsClient.MemberMatching(pending));

        var result = await NewHandler(db, facets)
            .Handle(new CompleteRegistrationCommand(pending.Email, Ssn, null), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(RegistrationErrors.PasswordNotSet, result.Error);
        Assert.Empty(db.Users);
    }

    [Fact]
    public async Task Refuses_when_there_is_no_pending_registration()
    {
        await using var db = TestDoubles.NewDb();
        var facets = new FakeFacetsClient(RegistrationErrors.MemberNotFound);

        var result = await NewHandler(db, facets)
            .Handle(new CompleteRegistrationCommand("nobody@example.com", Ssn, null), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(RegistrationErrors.RegistrationNotFoundOrExpired, result.Error);
    }

    [Fact]
    public async Task Passes_through_a_facets_lookup_failure_without_creating_a_user()
    {
        await using var db = TestDoubles.NewDb();
        var pending = await SeedPendingAsync(db);
        var facets = new FakeFacetsClient(RegistrationErrors.EligibilityLookupFailed);

        var result = await NewHandler(db, facets)
            .Handle(new CompleteRegistrationCommand(pending.Email, Ssn, null), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(RegistrationErrors.EligibilityLookupFailed, result.Error);
        Assert.Empty(db.Users);
        // The pending record survives, so the member can retry once Facets is back.
        Assert.Single(db.PendingRegistrations);
    }

    [Fact]
    public async Task Rejects_a_facets_match_whose_surname_disagrees()
    {
        await using var db = TestDoubles.NewDb();
        var pending = await SeedPendingAsync(db, lastName: "Doe");
        var someoneElse = FakeFacetsClient.MemberMatching(pending) with { LastName = "Different" };

        var result = await NewHandler(db, new FakeFacetsClient(someoneElse))
            .Handle(new CompleteRegistrationCommand(pending.Email, Ssn, null), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Empty(db.Users);
        // Indistinguishable from "not found" on purpose — otherwise the response confirms whose SSN
        // this is.
        Assert.Equal(RegistrationErrors.MemberNotFound.Code, result.Error.Code);
        Assert.Equal(RegistrationErrors.MemberNotFound.Description, result.Error.Description);
    }

    [Fact]
    public async Task Rejects_a_facets_match_whose_date_of_birth_disagrees()
    {
        await using var db = TestDoubles.NewDb();
        var pending = await SeedPendingAsync(db);
        var someoneElse = FakeFacetsClient.MemberMatching(pending) with
        {
            DateOfBirth = new DateOnly(1971, 1, 1)
        };

        var result = await NewHandler(db, new FakeFacetsClient(someoneElse))
            .Handle(new CompleteRegistrationCommand(pending.Email, Ssn, null), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Empty(db.Users);
    }

    [Fact]
    public async Task Matches_a_surname_that_differs_only_by_case_or_spacing()
    {
        await using var db = TestDoubles.NewDb();
        var pending = await SeedPendingAsync(db, lastName: "Doe");
        var member = FakeFacetsClient.MemberMatching(pending) with { LastName = " DOE " };

        var result = await NewHandler(db, new FakeFacetsClient(member))
            .Handle(new CompleteRegistrationCommand(pending.Email, Ssn, null), CancellationToken.None);

        Assert.True(result.IsSuccess);
    }
}
