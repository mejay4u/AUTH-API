using System.Security.Cryptography.X509Certificates;
using AuthApi.Application.Common.Interfaces;
using AuthApi.Infrastructure.Gateways.Apigee;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace AuthApi.Infrastructure.Gateways;

/// <summary>
/// Composition root for the pass-through path: one typed <see cref="HttpClient"/> pointed at Apigee
/// Internal, wrapped in the two delegating handlers that make the relay safe to run in production.
/// </summary>
public static class GatewayServiceCollectionExtensions
{
    public static IServiceCollection AddApigeeInternalGateway(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<ApigeeInternalOptions>()
            .Bind(configuration.GetSection(ApigeeInternalOptions.SectionName))
            .ValidateDataAnnotations()
            .Validate(
                o => Uri.TryCreate(o.BaseAddress, UriKind.Absolute, out var uri) &&
                     (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp),
                "ApigeeInternal:BaseAddress must be an absolute http(s) URI.");

        services.AddTransient<CorrelationIdHandler>();
        services.AddTransient<TransientFaultHandler>();

        services
            .AddHttpClient<IUpstreamAuthGateway, ApigeeInternalAuthGateway>(ConfigureClient)
            .ConfigurePrimaryHttpMessageHandler(CreatePrimaryHandler)
            // Order matters: the retry handler sits INSIDE the correlation handler, so every attempt
            // of a retried call carries the same correlation id and ARTS can collapse them in its logs.
            .AddHttpMessageHandler<CorrelationIdHandler>()
            .AddHttpMessageHandler<TransientFaultHandler>()
            // Recycle pooled connections so the pod picks up DNS changes when Apigee Internal moves.
            .SetHandlerLifetime(TimeSpan.FromMinutes(5));

        return services;
    }

    private static void ConfigureClient(IServiceProvider serviceProvider, HttpClient client)
    {
        var options = serviceProvider.GetRequiredService<IOptions<ApigeeInternalOptions>>().Value;

        // A trailing slash makes the relative Login/Refresh paths resolve under the proxy's base path
        // instead of replacing it.
        var baseAddress = options.BaseAddress.EndsWith('/') ? options.BaseAddress : options.BaseAddress + "/";

        client.BaseAddress = new Uri(baseAddress, UriKind.Absolute);

        // Budget for the whole call including retries; the gateway maps the resulting cancellation
        // onto a 504 rather than letting it surface as an unhandled exception.
        client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);

        client.DefaultRequestHeaders.Accept.Clear();
        client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
    }

    private static HttpMessageHandler CreatePrimaryHandler(IServiceProvider serviceProvider)
    {
        var options = serviceProvider.GetRequiredService<IOptions<ApigeeInternalOptions>>().Value;

        var handler = new SocketsHttpHandler
        {
            // Below the handler lifetime, so idle connections are reaped before the pool rotates.
            PooledConnectionLifetime = TimeSpan.FromMinutes(2),
            AutomaticDecompression = System.Net.DecompressionMethods.All,

            // The BFA relays upstream status codes verbatim, so a 302 from Apigee must reach the
            // client as a 302 rather than being silently chased with our credentials attached.
            AllowAutoRedirect = false
        };

        if (!string.IsNullOrWhiteSpace(options.ClientCertificatePath))
        {
            var certificate = new X509Certificate2(
                options.ClientCertificatePath,
                options.ClientCertificatePassword);

            handler.SslOptions.ClientCertificates ??= new X509CertificateCollection();
            handler.SslOptions.ClientCertificates.Add(certificate);
        }

        return handler;
    }
}
