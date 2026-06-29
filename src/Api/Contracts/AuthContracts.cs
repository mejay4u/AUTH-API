using AuthApi.Application.Common.Models;

namespace AuthApi.Api.Contracts;

/// <summary>
/// Login request body sent by the Member Portal. <paramref name="Lob"/> selects which line-of-business
/// database to authenticate against (e.g. "DENTAL", "VISION", "MEDICAL", "RX").
/// </summary>
public sealed record LoginRequest(string Username, string Password, string Lob);

/// <summary>Refresh request body — exchanges a refresh token for a new access token.</summary>
public sealed record RefreshRequest(string RefreshToken);

/// <summary>Token response returned to the client.</summary>
public sealed record AuthResponse(
    Guid MemberId,
    string Username,
    string TokenType,
    string AccessToken,
    DateTime AccessTokenExpiresUtc,
    string RefreshToken,
    DateTime RefreshTokenExpiresUtc,
    IReadOnlyCollection<string> Lobs,
    IReadOnlyCollection<int> PlanIds)
{
    public static AuthResponse From(AuthenticationResult r) => new(
        r.MemberId,
        r.Username,
        r.TokenType,
        r.AccessToken,
        r.AccessTokenExpiresUtc,
        r.RefreshToken,
        r.RefreshTokenExpiresUtc,
        r.Lobs,
        r.PlanIds);
}
