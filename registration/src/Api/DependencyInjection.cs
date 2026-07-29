using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.FeatureManagement;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Registration.Api.Endpoints;
using Registration.Api.Infrastructure;

namespace Registration.Api;

public static class DependencyInjection
{
    private const string CorsPolicy = "MemberPortal";

    public static IServiceCollection AddApiServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddProblemDetails();
        services.AddExceptionHandler<GlobalExceptionHandler>();

        // Feature flags (config section "FeatureManagement"), used to gate the registration endpoints.
        services.AddFeatureManagement();

        AddDescopeAuthentication(services, configuration);
        AddRateLimiting(services, configuration);
        AddCors(services, configuration);
        AddSwagger(services);

        return services;
    }

    /// <summary>
    /// Validates the Descope session token the app sends. Signature comes from Descope's JWKS for the
    /// project; the issuer is the project ID. There is no audience to check — Descope doesn't set one
    /// for session tokens by default — so issuer + signature + lifetime are what we rely on.
    /// </summary>
    private static void AddDescopeAuthentication(IServiceCollection services, IConfiguration configuration)
    {
        var options = configuration.GetSection(DescopeAuthOptions.SectionName).Get<DescopeAuthOptions>()
                      ?? new DescopeAuthOptions();

        services.AddOptions<DescopeAuthOptions>()
            .Bind(configuration.GetSection(DescopeAuthOptions.SectionName))
            .Validate(
                o => o.AllowAnonymous || !string.IsNullOrWhiteSpace(o.ProjectId),
                "Descope:ProjectId is required unless Descope:AllowAnonymous is true.")
            .ValidateOnStart();

        if (options.AllowAnonymous)
        {
            // Nothing to wire up — the endpoints skip RequireAuthorization in this mode.
            return;
        }

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(jwt =>
            {
                jwt.MetadataAddress = options.MetadataAddress;
                jwt.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = options.ProjectId,
                    ValidateAudience = false,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    // Tokens are short-lived; don't hand out extra grace.
                    ClockSkew = TimeSpan.FromSeconds(30)
                };
            });

        services.AddAuthorization();
    }

    private static void AddRateLimiting(IServiceCollection services, IConfiguration configuration)
    {
        var permitLimit = configuration.GetValue("RateLimiting:PermitPerMinute", 20);

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.AddPolicy(RateLimiterPolicies.Registration, httpContext =>
            {
                var ip = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                return RateLimitPartition.GetFixedWindowLimiter(ip, _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = permitLimit,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0,
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst
                });
            });
        });
    }

    private static void AddCors(IServiceCollection services, IConfiguration configuration)
    {
        var origins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];

        services.AddCors(options => options.AddPolicy(CorsPolicy, policy =>
        {
            if (origins.Length > 0)
            {
                policy.WithOrigins(origins)
                      .AllowAnyHeader()
                      .WithMethods("POST")
                      .AllowCredentials();
            }
        }));
    }

    private static void AddSwagger(IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "Member Registration API",
                Version = "v1",
                Description =
                    "Stores the member record and password for the Member Portal sign-up wizard. "
                    + "Email verification is done by Descope before these endpoints are called."
            });
        });
    }

    public static string CorsPolicyName => CorsPolicy;
}
