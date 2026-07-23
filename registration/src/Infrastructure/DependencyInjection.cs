using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Registration.Application.Common.Interfaces;
using Registration.Application.Common.Options;
using Registration.Infrastructure.Email;
using Registration.Infrastructure.Otp;
using Registration.Infrastructure.Persistence;
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
        AddUserStore(services, configuration, isDevelopment);

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

    private static void AddUserStore(IServiceCollection services, IConfiguration configuration, bool isDevelopment)
    {
        // Writes into the EXISTING user table. InMemory mock in Development; Dapper against the real
        // member portal DB otherwise. No new tables are created in either mode.
        var provider = configuration.GetValue<string>("UserStore:Provider")
                       ?? (isDevelopment ? "InMemory" : "SqlServer");

        if (provider.Equals("SqlServer", StringComparison.OrdinalIgnoreCase))
        {
            services.AddScoped<IUserRegistrationRepository, DapperUserRegistrationRepository>();
        }
        else
        {
            services.AddSingleton<IUserRegistrationRepository, InMemoryUserRegistrationRepository>();
        }
    }
}
