namespace Registration.Application.Common.Models;

/// <summary>
/// The minimal profile the legacy verifier returns for a successfully verified legacy member. Used to
/// seed the migrated <c>User</c> record.
/// </summary>
public sealed record LegacyMemberProfile(
    string Email,
    string FirstName,
    string LastName,
    string LegacyMemberId,
    DateOnly? DateOfBirth,
    string? ZipCode,
    string? ContactNumber);
