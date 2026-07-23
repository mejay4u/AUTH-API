namespace Registration.Application.Common.Models;

/// <summary>
/// The data written to the existing user table when creating a new portal account.
/// <see cref="Username"/> equals the email address (email is the User ID / login).
/// </summary>
public sealed record NewUserRegistration(
    string Email,
    string Username,
    string PasswordHash,
    string PasswordSalt);
