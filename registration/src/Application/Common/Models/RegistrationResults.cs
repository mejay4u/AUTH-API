namespace Registration.Application.Common.Models;

/// <summary>
/// The result of <c>initiateRegistration</c> — the identifier Descope holds onto for the rest of the
/// flow, plus the state the record is in.
/// </summary>
public sealed record InitiateRegistrationResult(Guid UserId, string Email, string Status)
{
    public const string PendingStatus = "Pending";
}

/// <summary>
/// The result of <c>completeRegistration</c>. Descope maps <see cref="MemberInfo.SubscriberId"/> and
/// <see cref="PlanInfo.PlanId"/> into the session JWT's custom claims, so those two names are part of
/// the contract with the flow — renaming them means reconfiguring the connector.
/// </summary>
public sealed record CompleteRegistrationResult(
    bool Complete,
    Guid UserId,
    MemberInfo MemberInfo,
    PlanInfo PlanInfo);

/// <summary>The confirmed member, as returned to the flow. Carries no SSN.</summary>
public sealed record MemberInfo(
    string SubscriberId,
    string MemberId,
    string FirstName,
    string LastName,
    string Email);

/// <summary>The confirmed plan, as returned to the flow.</summary>
public sealed record PlanInfo(
    string PlanId,
    string PlanName,
    string? LineOfBusiness,
    DateOnly? EffectiveDate);
