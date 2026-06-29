namespace AuthApi.Application.Common.Models;

/// <summary>
/// Flattened login data for a member, returned by <c>IAccountRepository.GetMemberPortalLoginData…</c>.
/// This is the shape the legacy <c>AccountRepository.GetMemberPortalLoginData</c> produced — a single
/// read model with everything the login use case needs (credentials to verify + the LOB/Plan claims to
/// embed in the JWT). It deliberately does NOT expose EF entities, so the handler stays persistence-agnostic.
/// </summary>
public sealed record MemberPortalLoginData(
    Guid MemberId,
    string Username,
    string PasswordHash,
    string PasswordSalt,
    bool IsActive,
    string? Email,
    string? FirstName,
    string? LastName,
    IReadOnlyCollection<string> Lobs,
    IReadOnlyCollection<int> PlanIds);
