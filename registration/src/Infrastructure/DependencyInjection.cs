using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Registration.Application.Common.Interfaces;
using Registration.Infrastructure.Legacy;
using Registration.Infrastructure.Persistence;
using Registration.Infrastructure.Persistence.Repositories;

namespace Registration.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration,
        bool isDevelopment)
    {
        services.AddSingleton(TimeProvider.System);

        AddPersistence(services, configuration, isDevelopment);
        AddLegacyVerifier(services, configuration, isDevelopment);

        return services;
    }

    private static void AddPersistence(IServiceCollection services, IConfiguration configuration, bool isDevelopment)
    {
        // Registration owns its own database (database-first: schema authored in SQL; EF maps to it).
        var provider = configuration.GetValue<string>("Database:Provider")
                       ?? (isDevelopment ? "InMemory" : "SqlServer");

        services.AddDbContext<RegistrationDbContext>(options =>
        {
            if (provider.Equals("SqlServer", StringComparison.OrdinalIgnoreCase))
            {
                var connectionString = configuration.GetConnectionString("RegistrationDb")
                    ?? throw new InvalidOperationException(
                        "ConnectionStrings:RegistrationDb is required when Database:Provider=SqlServer.");

                options.UseSqlServer(connectionString, sql => sql.EnableRetryOnFailure());
            }
            else
            {
                options.UseInMemoryDatabase("RegistrationMockDb");
            }
        });

        services.AddScoped<IUserProfileRepository, EfUserProfileRepository>();
        services.AddScoped<IMigrationAuditRepository, EfMigrationAuditRepository>();
    }

    private static void AddLegacyVerifier(IServiceCollection services, IConfiguration configuration, bool isDevelopment)
    {
        services.AddOptions<LegacyVerifyApiOptions>()
            .Bind(configuration.GetSection(LegacyVerifyApiOptions.SectionName))
            .ValidateOnStart();

        // Mock in Development (or when explicitly enabled) so the JIT flow runs without the legacy API.
        var useMock = configuration.GetValue($"{LegacyVerifyApiOptions.SectionName}:UseMock", isDevelopment);
        if (useMock)
        {
            services.AddSingleton<ILegacyMemberVerifier, MockLegacyMemberVerifier>();
        }
        else
        {
            services.AddHttpClient<ILegacyMemberVerifier, HttpLegacyMemberVerifier>((sp, client) =>
            {
                var options = sp.GetRequiredService<IOptions<LegacyVerifyApiOptions>>().Value;
                client.BaseAddress = new Uri(options.BaseUrl);
                if (!string.IsNullOrEmpty(options.ApiKey))
                {
                    client.DefaultRequestHeaders.Add("X-Api-Key", options.ApiKey);
                }
            });
        }
    }
}
