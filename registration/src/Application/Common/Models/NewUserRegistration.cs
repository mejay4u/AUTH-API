namespace Registration.Application.Common.Models;

/// <summary>
/// The data written to the registration database when creating a new portal account.
/// <see cref="Username"/> equals the email address (email is the User ID / login), plus the personal
/// information collected on the registration screen.
/// </summary>
public sealed record NewUserRegistration(
    string Email,
    string Username,
    string PasswordHash,
    string PasswordSalt,
    string FirstName,
    string LastName,
    DateOnly DateOfBirth,
    string ZipCode,
    string? ContactNumber);
