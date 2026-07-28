using Registration.Application.Common.Models;

namespace Registration.Api.Contracts;

/// <summary>
/// Phase 2: Descope has verified the email and passes on what it collected in phase 1.
/// <c>DateOfBirth</c> is an ISO date (yyyy-MM-dd).
/// </summary>
public sealed record InitiateRegistrationRequest(
    string Email,
    string FirstName,
    string LastName,
    DateOnly DateOfBirth,
    string ZipCode);

/// <summary>
/// The identifier Descope holds for the rest of the flow. Named <c>UserId</c> because that is what the
/// sequence diagram calls it, even though the underlying record is still pending.
/// </summary>
public sealed record InitiateRegistrationResponse(Guid UserId, string Email, string Status)
{
    public static InitiateRegistrationResponse From(InitiateRegistrationResult r) =>
        new(r.UserId, r.Email, r.Status);
}

/// <summary>Phase 3: the password the member chose, with its confirmation.</summary>
public sealed record SetPasswordRequest(Guid UserId, string Password, string ConfirmPassword);

/// <summary>Phase 4: the details used to confirm membership against Facets.</summary>
public sealed record CompleteRegistrationRequest(string Email, string Ssn, string? MemberId);

/// <summary>
/// Phase 4's result. Descope maps <c>memberInfo.subscriberId</c> and <c>planInfo.planId</c> into the
/// session JWT's custom claims, so those paths are part of the contract with the flow.
/// </summary>
public sealed record CompleteRegistrationResponse(
    bool Complete,
    Guid UserId,
    MemberInfo MemberInfo,
    PlanInfo PlanInfo)
{
    public static CompleteRegistrationResponse From(CompleteRegistrationResult r) =>
        new(r.Complete, r.UserId, r.MemberInfo, r.PlanInfo);
}

/// <summary>Generic success message where there is nothing else to return.</summary>
public sealed record MessageResponse(string Message);
