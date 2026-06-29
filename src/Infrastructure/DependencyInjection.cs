using AuthApi.Application.Common.Interfaces;
using AuthApi.Application.Common.Security;
using AuthApi.Infrastructure.Persistence;
using AuthApi.Infrastructure.Persistence.Repositories;
using AuthApi.Infrastructure.Security.Jwt;
using AuthApi.Infrastructure.Security.PasswordHashing;
using AuthApi.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AuthApi.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration,
        bool isDevelopment)
    {
        AddOptions(services, configuration, isDevelopment);
        AddPersistence(services, configuration);

        services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();
        services.AddScoped<IPasswordHasher, SaltedHashPasswordHasher>();

        // Data-access boundary for login data (legacy AccountRepository equivalent, database-first).
        services.AddScoped<IAccountRepository, AccountRepository>();

        // The RSA key must be stable for the process lifetime -> singleton.
        services.AddSingleton<RsaSigningKeyProvider>();
        services.AddScoped<ITokenService, TokenService>();

        services.AddScoped<AuthDbDataSeeder>();

        return services;
    }

    private static void AddOptions(IServiceCollection services, IConfiguration configuration, bool isDevelopment)
    {
        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .Configure(o => o.AllowEphemeralKey = isDevelopment)
            .ValidateDataAnnotations();

        services.AddOptions<PasswordHashingOptions>()
            .Bind(configuration.GetSection(PasswordHashingOptions.SectionName));

        services.AddOptions<AccountLockoutOptions>()
            .Bind(configuration.GetSection(AccountLockoutOptions.SectionName))
            .ValidateDataAnnotations();
    }

    private static void AddPersistence(IServiceCollection services, IConfiguration configuration)
    {
        var provider = configuration.GetValue<string>("Database:Provider") ?? "InMemory";

        services.AddDbContext<AuthDbContext>(options =>
        {
            if (provider.Equals("SqlServer", StringComparison.OrdinalIgnoreCase))
            {
                // Real existing member database. Drop in the connection string to switch over —
                // no other code changes required.
                var connectionString = configuration.GetConnectionString("AuthDb")
                    ?? throw new InvalidOperationException("ConnectionStrings:AuthDb is required when Database:Provider=SqlServer.");

                options.UseSqlServer(connectionString, sql =>
                {
                    sql.EnableRetryOnFailure();
                    sql.MigrationsAssembly(typeof(AuthDbContext).Assembly.FullName);
                });
            }
            else
            {
                // Mock store: in-memory database seeded with sample members/LOBs/plans.
                options.UseInMemoryDatabase("AuthApiMockDb");
            }
        });

        services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<AuthDbContext>());
    }
}
