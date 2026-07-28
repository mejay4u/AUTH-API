using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Registration.Application.Common.Interfaces;
using Registration.Application.Common.Options;
using Registration.Infrastructure.Facets;
using Registration.Infrastructure.Persistence;
using Registration.Infrastructure.Persistence.Repositories;
using Registration.Infrastructure.Security.PasswordHashing;

namespace Registration.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration,
        bool isDevelopment)
    {
        AddOptions(services, configuration);

        services.AddSingleton(TimeProvider.System);

        services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();

        AddFacets(services, isDevelopment);
        AddPersistence(services, configuration, isDevelopment);

        return services;
    }

    private static void AddOptions(IServiceCollection services, IConfiguration configuration)
    {
        // Password policy lives in Application (the validator reads it); bound here.
        services.AddOptions<PasswordPolicyOptions>()
            .Bind(configuration.GetSection(PasswordPolicyOptions.SectionName))
            .ValidateOnStart();

        services.AddOptions<RegistrationOptions>()
            .Bind(configuration.GetSection(RegistrationOptions.SectionName))
            .ValidateOnStart();

        services.AddOptions<PasswordHashingOptions>()
            .Bind(configuration.GetSection(PasswordHashingOptions.SectionName))
            .ValidateOnStart();

        services.AddOptions<FacetsOptions>()
            .Bind(configuration.GetSection(FacetsOptions.SectionName))
            .ValidateOnStart();
    }

    private static void AddFacets(IServiceCollection services, bool isDevelopment)
    {
        services.AddSingleton<IFacetsClient>(provider =>
        {
            var facets = provider.GetRequiredService<IOptions<FacetsOptions>>().Value;

            // The stub matches everyone, so it is only ever allowed in Development — a misconfigured
            // environment must fail loudly rather than quietly hand out accounts.
            if (facets.UseStub)
            {
                if (!isDevelopment)
                {
                    throw new InvalidOperationException(
                        "Facets:Provider = Stub is only permitted in Development.");
                }

                return ActivatorUtilities.CreateInstance<StubFacetsClient>(provider);
            }

            if (string.IsNullOrWhiteSpace(facets.BaseUrl))
            {
                throw new InvalidOperationException(
                    "Facets:BaseUrl is required when Facets:Provider = Http.");
            }

            // A single long-lived HttpClient with a fixed base address. If the team adds
            // Microsoft.Extensions.Http, swap this for AddHttpClient<IFacetsClient, HttpFacetsClient>()
            // to pick up handler rotation and Polly policies.
            var httpClient = new HttpClient
            {
                BaseAddress = new Uri(facets.BaseUrl, UriKind.Absolute),
                Timeout = TimeSpan.FromSeconds(facets.TimeoutSeconds)
            };

            if (!string.IsNullOrWhiteSpace(facets.ApiKey))
            {
                httpClient.DefaultRequestHeaders.Add("X-Api-Key", facets.ApiKey);
            }

            return ActivatorUtilities.CreateInstance<HttpFacetsClient>(provider, httpClient);
        });
    }

    private static void AddPersistence(IServiceCollection services, IConfiguration configuration, bool isDevelopment)
    {
        // Registration owns its own database (database-first: the schema is authored in SQL, EF maps to
        // it and does not migrate). SqlServer for the real DB; InMemory in Development so the flow runs
        // without a database.
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

        services.AddScoped<IUserRegistrationRepository, EfUserRegistrationRepository>();
        services.AddScoped<IPendingRegistrationRepository, EfPendingRegistrationRepository>();
    }
}
