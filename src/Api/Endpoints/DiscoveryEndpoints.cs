using AuthApi.Infrastructure.Security.Jwt;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace AuthApi.Api.Endpoints;

/// <summary>
/// Publishes the public signing material so OTHER microservices can validate tokens issued here
/// without ever holding a secret. They can point their JwtBearer "Authority"/MetadataAddress at
/// these well-known endpoints.
/// </summary>
public static class DiscoveryEndpoints
{
    public static IEndpointRouteBuilder MapDiscoveryEndpoints(this IEndpointRouteBuilder app)
    {
        // JWKS — the RSA public key(s) as a JSON Web Key Set.
        app.MapGet("/.well-known/jwks.json", (RsaSigningKeyProvider keys) =>
            {
                var jwks = keys.GetJsonWebKeySet();
                var payload = new
                {
                    keys = jwks.Keys.Select(k => new
                    {
                        kty = k.Kty,
                        use = k.Use,
                        alg = k.Alg,
                        kid = k.Kid,
                        n = k.N,
                        e = k.E
                    })
                };
                return Results.Json(payload, contentType: "application/json");
            })
            .AllowAnonymous()
            .WithTags("Discovery")
            .WithName("Jwks");

        // Minimal OIDC-style discovery document pointing at the JWKS.
        app.MapGet("/.well-known/openid-configuration",
                (HttpContext ctx, IOptions<JwtOptions> jwt) =>
                {
                    var baseUrl = $"{ctx.Request.Scheme}://{ctx.Request.Host}";
                    return Results.Json(new
                    {
                        issuer = jwt.Value.Issuer,
                        jwks_uri = $"{baseUrl}/.well-known/jwks.json",
                        id_token_signing_alg_values_supported = new[] { SecurityAlgorithms.RsaSha256 },
                        response_types_supported = new[] { "token" }
                    });
                })
            .AllowAnonymous()
            .WithTags("Discovery")
            .WithName("OpenIdConfiguration");

        return app;
    }
}
