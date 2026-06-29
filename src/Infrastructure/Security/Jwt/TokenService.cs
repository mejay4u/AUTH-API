using System.Security.Claims;
using System.Security.Cryptography;
using AuthApi.Application.Common.Interfaces;
using AuthApi.Application.Common.Models;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace AuthApi.Infrastructure.Security.Jwt;

/// <summary>
/// Issues RS256-signed access tokens and cryptographically-random refresh tokens.
/// Custom claims <c>lob</c> and <c>plan</c> are emitted once per association so downstream services
/// can authorize on a member's lines of business and plans.
/// </summary>
public sealed class TokenService(
    RsaSigningKeyProvider keyProvider,
    IOptions<JwtOptions> options,
    IDateTimeProvider clock) : ITokenService
{
    public const string LobClaimType = "lob";
    public const string PlanClaimType = "plan";

    private readonly JwtOptions _options = options.Value;

    public AccessToken CreateAccessToken(MemberPortalLoginData member)
    {
        var now = clock.UtcNow;
        var expires = now.Add(_options.AccessTokenLifetime);
        var jti = Guid.NewGuid().ToString("N");

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, member.MemberId.ToString()),
            new(JwtRegisteredClaimNames.Jti, jti),
            new(JwtRegisteredClaimNames.UniqueName, member.Username),
            new(ClaimTypes.NameIdentifier, member.MemberId.ToString())
        };

        if (!string.IsNullOrWhiteSpace(member.Email))
        {
            claims.Add(new Claim(JwtRegisteredClaimNames.Email, member.Email));
        }

        if (!string.IsNullOrWhiteSpace(member.FirstName))
        {
            claims.Add(new Claim(JwtRegisteredClaimNames.GivenName, member.FirstName));
        }

        if (!string.IsNullOrWhiteSpace(member.LastName))
        {
            claims.Add(new Claim(JwtRegisteredClaimNames.FamilyName, member.LastName));
        }

        claims.AddRange(member.Lobs.Select(code => new Claim(LobClaimType, code)));
        claims.AddRange(member.PlanIds.Select(id => new Claim(PlanClaimType, id.ToString(), ClaimValueTypes.Integer32)));

        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Issuer = _options.Issuer,
            Audience = _options.Audience,
            IssuedAt = now,
            NotBefore = now,
            Expires = expires,
            SigningCredentials = keyProvider.SigningCredentials,
            TokenType = "at+jwt"
        };

        var handler = new JsonWebTokenHandler { SetDefaultTimesOnTokenCreation = false };
        var token = handler.CreateToken(descriptor);

        return new AccessToken(token, expires, jti);
    }

    public RefreshTokenValue CreateRefreshToken()
    {
        // 256 bits of entropy, URL-safe. The raw value is shown to the client once; only its hash is stored.
        var raw = Base64UrlEncoder.Encode(RandomNumberGenerator.GetBytes(32));
        var hash = HashRefreshToken(raw);
        var expires = clock.UtcNow.Add(_options.RefreshTokenLifetime);
        return new RefreshTokenValue(raw, hash, expires);
    }

    public string HashRefreshToken(string rawToken)
    {
        var hash = SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(rawToken));
        return Base64UrlEncoder.Encode(hash);
    }
}
