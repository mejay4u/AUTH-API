namespace AuthApi.Application.Common.Models;

/// <summary>A signed access token and its metadata.</summary>
public sealed record AccessToken(string Value, DateTime ExpiresUtc, string Jti);

/// <summary>A freshly minted refresh token: the raw value returned to the client and the hash to persist.</summary>
public sealed record RefreshTokenValue(string Raw, string Hash, DateTime ExpiresUtc);

/// <summary>
/// The authentication result returned by the Login/Refresh use cases and serialized by the API.
/// </summary>
public sealed record AuthenticationResult(
    Guid MemberId,
    string Username,
    string AccessToken,
    DateTime AccessTokenExpiresUtc,
    string RefreshToken,
    DateTime RefreshTokenExpiresUtc,
    IReadOnlyCollection<string> Lobs,
    IReadOnlyCollection<int> PlanIds)
{
    public string TokenType => "Bearer";
}
