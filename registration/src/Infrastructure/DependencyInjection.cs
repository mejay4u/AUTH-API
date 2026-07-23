using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Registration.Application.Common.Interfaces;
using Registration.Application.Common.Options;
using Registration.Infrastructure.Email;
using Registration.Infrastructure.Otp;
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

        services.AddMemoryCache();
        services.AddSingleton(TimeProvider.System);

        services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();
        services.AddSingleton<IOtpService, OtpService>();

        AddEmailSender(services, isDevelopment);
        AddPersistence(services, configuration, isDevelopment);

        return services;
    }

    private static void AddOptions(IServiceCollection services, IConfiguration configuration)
    {
        // Password policy + OTP options live in Application (validators read them); bound here.
        services.AddOptions<PasswordPolicyOptions>()
            .Bind(configuration.GetSection(PasswordPolicyOptions.SectionName))
            .ValidateOnStart();

        services.AddOptions<OtpOptions>()
            .Bind(configuration.GetSection(OtpOptions.SectionName))
            .ValidateOnStart();

        services.AddOptions<PasswordHashingOptions>()
            .Bind(configuration.GetSection(PasswordHashingOptions.SectionName))
            .ValidateOnStart();

        services.AddOptions<SmtpOptions>()
            .Bind(configuration.GetSection(SmtpOptions.SectionName))
            .ValidateOnStart();
    }

    private static void AddEmailSender(IServiceCollection services, bool isDevelopment)
    {
        // Real SMTP outside Development; a logging no-op in Development so the flow runs mail-server-free.
        if (isDevelopment)
        {
            services.AddSingleton<IEmailSender, LoggingEmailSender>();
        }
        else
        {
            services.AddSingleton<IEmailSender, SmtpEmailSender>();
        }
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
    }
}
