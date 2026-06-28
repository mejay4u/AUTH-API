using AuthApi.Domain.Common;

namespace AuthApi.Domain.Members;

/// <summary>
/// Central catalogue of authentication errors. Note the deliberate use of a single, generic
/// "invalid credentials" error for unknown user / wrong password / inactive account — this avoids
/// <b>user enumeration</b> (an attacker cannot tell whether a username exists).
/// </summary>
public static class AuthErrors
{
    public static readonly Error InvalidCredentials =
        Error.Unauthorized("Auth.InvalidCredentials", "Invalid username or password.");

    public static readonly Error AccountLocked =
        Error.Forbidden("Auth.AccountLocked", "The account is temporarily locked. Try again later.");

    public static readonly Error InvalidRefreshToken =
        Error.Unauthorized("Auth.InvalidRefreshToken", "The refresh token is invalid or has expired.");
}
