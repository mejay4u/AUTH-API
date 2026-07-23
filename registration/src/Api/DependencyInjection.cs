using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.FeatureManagement;
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

        AddRateLimiting(services, configuration);
        AddCors(services, configuration);
        AddSwagger(services);

        return services;
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
                Description = "Email-OTP verification and portal user creation for the Member Portal."
            });
        });
    }

    public static string CorsPolicyName => CorsPolicy;
}
