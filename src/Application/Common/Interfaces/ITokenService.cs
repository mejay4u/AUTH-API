using AuthApi.Application.Common.Models;

namespace AuthApi.Application.Common.Interfaces;

/// <summary>
/// Issues signed access tokens and opaque refresh tokens. The implementation in Infrastructure
/// signs access tokens with an asymmetric (RSA / RS256) key so downstream microservices can
/// validate them with only the public key — they never hold any signing secret.
/// </summary>
public interface ITokenService
{
    /// <summary>Create a short-lived signed JWT access token containing the member's LOBs and Plan IDs.</summary>
    AccessToken CreateAccessToken(MemberPortalLoginData member);

    /// <summary>Create a cryptographically-random refresh token (raw value + its stored hash).</summary>
    RefreshTokenValue CreateRefreshToken();

    /// <summary>Hash a raw refresh token the same way it is stored, for lookup/verification.</summary>
    string HashRefreshToken(string rawToken);
}
