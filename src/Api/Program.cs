using AuthApi.Api;
using AuthApi.Api.Endpoints;
using AuthApi.Api.Infrastructure;
using AuthApi.Application;
using AuthApi.Infrastructure;
using AuthApi.Infrastructure.Gateways.Apigee;
using AuthApi.Infrastructure.Persistence;
using AuthApi.Infrastructure.Security.Jwt;
using Microsoft.Extensions.Options;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Local issuance vs. BFA pass-through. Read straight from configuration because the whole composition
// root branches on it — which services exist, which middleware runs, which endpoints are mapped.
var isPassThrough = builder.Configuration.IsPassThrough();

builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext());

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration, builder.Environment.IsDevelopment());
builder.Services.AddApiServices(builder.Configuration);

var app = builder.Build();

// Fail fast: validate whatever this mode cannot run without, before serving traffic.
using (var scope = app.Services.CreateScope())
{
    if (isPassThrough)
    {
        // Touching .Value runs the DataAnnotations and custom validators on ApigeeInternalOptions, so
        // a pod with a missing or malformed upstream address never reaches the readiness probe.
        var apigee = scope.ServiceProvider.GetRequiredService<IOptions<ApigeeInternalOptions>>().Value;

        app.Logger.LogInformation(
            "Auth pass-through enabled: relaying to Apigee Internal at {BaseAddress} (timeout {TimeoutSeconds}s).",
            apigee.BaseAddress, apigee.TimeoutSeconds);
    }
    else
    {
        // Construct the signing key (and validate Jwt options) before serving traffic.
        scope.ServiceProvider.GetRequiredService<RsaSigningKeyProvider>();

        var seedMockData = app.Configuration.GetValue("Database:SeedMockData", app.Environment.IsDevelopment());
        if (seedMockData)
        {
            var seeder = scope.ServiceProvider.GetRequiredService<AuthDbDataSeeder>();
            await seeder.SeedAsync();
        }
    }
}

app.UseExceptionHandler();
app.UseSecurityHeaders();

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.UseHttpsRedirection();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseSerilogRequestLogging();

app.UseCors(AuthApi.Api.DependencyInjection.CorsPolicyName);

// Rate limiting stays on in both modes. Note it partitions on the connection's remote IP, which
// behind Apigee External is Apigee's address rather than the caller's — so in pass-through it acts
// as a blunt total-throughput cap on ARTS, not a per-client throttle. See docs/PassThrough-Architecture.md.
app.UseRateLimiter();

if (!isPassThrough)
{
    app.UseAuthentication();
    app.UseAuthorization();
}

app.MapAuthEndpoints();

// Member and discovery endpoints exist only where this process owns the tokens. In pass-through mode
// ARTS is the issuer, so publishing a JWKS here would advertise a key that signs nothing.
if (!isPassThrough)
{
    app.MapMemberEndpoints();
    app.MapDiscoveryEndpoints();
}

app.MapGet("/health", () => Results.Ok(new { status = "healthy" }))
    .WithTags("Health")
    .AllowAnonymous();

app.Run();

// Exposed for integration testing with WebApplicationFactory.
public partial class Program;
