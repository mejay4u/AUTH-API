namespace Registration.Application.Common.Models;

/// <summary>
/// The data written when promoting a completed registration into a portal user.
/// <see cref="Username"/> equals the email address (email is the User ID / login). <see cref="Id"/> is
/// the pending record's id, carried over so the identifier stays stable.
/// </summary>
public sealed record NewUserRegistration(
    Guid Id,
    string Email,
    string Username,
    string PasswordHash,
    string PasswordSalt,
    string FirstName,
    string LastName,
    DateOnly DateOfBirth,
    string ZipCode,
    string SubscriberId,
    string PlanId,
    string SsnLast4);
