namespace Registration.Application.Common.Models;

/// <summary>
/// What we send to Facets to find a member. The SSN is the primary identifier; the rest narrow the
/// search and are also what the returned record is checked against.
/// </summary>
public sealed record FacetsMemberLookup(
    string Ssn,
    string? MemberId,
    string FirstName,
    string LastName,
    DateOnly DateOfBirth,
    string ZipCode);

/// <summary>A member as Facets knows them, including which tenant they were found in.</summary>
public sealed record FacetsMember(
    string SubscriberId,
    string MemberId,
    string FirstName,
    string LastName,
    DateOnly DateOfBirth,
    string ZipCode,
    string Tenant,
    FacetsPlan Plan);

/// <summary>The member's plan. <see cref="PlanId"/> is mapped into the session JWT's custom claims.</summary>
public sealed record FacetsPlan(
    string PlanId,
    string PlanName,
    string? LineOfBusiness,
    DateOnly? EffectiveDate);
