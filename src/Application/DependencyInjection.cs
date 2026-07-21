using System.Reflection;
using AuthApi.Application.Common.Behaviors;
using AuthApi.Application.Sso;
using AuthApi.Application.Sso.Providers;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace AuthApi.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(assembly));

        services.AddValidatorsFromAssembly(assembly, includeInternalTypes: true);

        // Validation runs for every request before the handler.
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

        // One strategy per SSO integration — the replacement for the legacy switch(SSOName).
        services.AddScoped<ISsoUrlProvider, AbarcaSsoUrlProvider>();
        services.AddScoped<ISsoUrlProvider, HraSsoUrlProvider>();
        services.AddScoped<ISsoUrlProvider, PlanOfCareSsoUrlProvider>();
        services.AddScoped<ISsoUrlProvider, ChatSsoUrlProvider>();
        services.AddScoped<ISsoUrlProvider, SoftheonSsoUrlProvider>();
        services.AddScoped<ISsoUrlProvider, CertifiSsoUrlProvider>();
        services.AddScoped<ISsoUrlProvider, SdsSsoUrlProvider>();

        return services;
    }
}
