namespace Registration.Application.Common.Models;

/// <summary>
/// The result of <c>initiateRegistration</c> — the identifier the app holds for the rest of the
/// wizard, plus the state the record is in.
/// </summary>
public sealed record InitiateRegistrationResult(Guid UserId, string Email, string Status)
{
    public const string PendingStatus = "Pending";
}

/// <summary>The outcome of a successful account creation, returned to the client.</summary>
public sealed record CreateAccountResult(Guid UserId, string Email, string Username);

/// <summary>
/// The data written when promoting a pending registration into a portal user.
/// <see cref="Username"/> equals the email address (email is the User ID / login). <see cref="Id"/> is
/// the pending record's id, carried over so the identifier stays stable.
/// </summary>
public sealed record NewUserRegistration(
    Guid Id,
    string Email,
    string Username,
    string PasswordHash,
    string PasswordSalt,
    string? DescopeUserId,
    string FirstName,
    string LastName,
    DateOnly DateOfBirth,
    string ZipCode,
    string? ContactNumber);
