using Registration.Application.Common.Models;

namespace Registration.Api.Contracts;

/// <summary>Descope user-sync webhook payload (registration/profile update pushed to us).</summary>
public sealed record DescopeUserSyncRequest(
    string DescopeUserId,
    string Email,
    string FirstName,
    string LastName,
    DateOnly? DateOfBirth,
    string? ZipCode,
    string? ContactNumber);

/// <summary>JIT verify hook payload — Descope sends the credentials for a legacy first login.</summary>
public sealed record DescopeVerifyRequest(string Email, string Password);

/// <summary>Response to the verify hook — the member profile Descope should adopt.</summary>
public sealed record DescopeVerifyResponse(
    bool Verified,
    Guid UserId,
    string Email,
    string FirstName,
    string LastName,
    string Origin)
{
    public static DescopeVerifyResponse From(MemberProfile p) =>
        new(true, p.UserId, p.Email, p.FirstName, p.LastName, p.Origin);
}

/// <summary>Generic success message.</summary>
public sealed record MessageResponse(string Message);
