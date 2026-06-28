using AuthApi.Infrastructure.Security.Jwt;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace AuthApi.Api.Infrastructure;

/// <summary>
/// Configures JWT bearer validation from the same RSA key and options the issuer uses. Other
/// microservices would instead point at this API's JWKS endpoint, but for this API's own protected
/// endpoints we validate locally with the public key.
/// </summary>
public sealed class ConfigureJwtBearerOptions(
    RsaSigningKeyProvider keyProvider,
    IOptions<JwtOptions> jwtOptions) : IConfigureNamedOptions<JwtBearerOptions>
{
    private readonly JwtOptions _jwt = jwtOptions.Value;

    public void Configure(string? name, JwtBearerOptions options) => Configure(options);

    public void Configure(JwtBearerOptions options)
    {
        // Keep original claim names (sub, lob, plan) instead of remapping to legacy XML claim URIs.
        options.MapInboundClaims = false;
        options.TokenHandlers.Clear();
        options.TokenHandlers.Add(new JsonWebTokenHandler());

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = _jwt.Issuer,

            ValidateAudience = true,
            ValidAudience = _jwt.Audience,

            ValidateIssuerSigningKey = true,
            IssuerSigningKey = keyProvider.PublicKey,

            ValidateLifetime = true,
            RequireExpirationTime = true,
            RequireSignedTokens = true,
            ClockSkew = TimeSpan.FromMinutes(1),

            ValidAlgorithms = [SecurityAlgorithms.RsaSha256],

            NameClaimType = JwtRegisteredClaimNames.UniqueName,
            RoleClaimType = "role"
        };
    }
}
