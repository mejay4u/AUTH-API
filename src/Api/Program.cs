using AuthApi.Api;
using AuthApi.Api.Endpoints;
using AuthApi.Api.Infrastructure;
using AuthApi.Application;
using AuthApi.Infrastructure;
using AuthApi.Infrastructure.Persistence;
using AuthApi.Infrastructure.Security.Jwt;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext());

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration, builder.Environment.IsDevelopment());
builder.Services.AddApiServices(builder.Configuration);

var app = builder.Build();

// Fail fast: construct the signing key (and validate Jwt options) before serving traffic.
using (var scope = app.Services.CreateScope())
{
    scope.ServiceProvider.GetRequiredService<RsaSigningKeyProvider>();

    var seedMockData = app.Configuration.GetValue("Database:SeedMockData", app.Environment.IsDevelopment());
    if (seedMockData)
    {
        var seeder = scope.ServiceProvider.GetRequiredService<AuthDbDataSeeder>();
        await seeder.SeedAsync();
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

app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

app.MapAuthEndpoints();
app.MapMemberEndpoints();
app.MapDiscoveryEndpoints();

app.MapGet("/health", () => Results.Ok(new { status = "healthy" }))
    .WithTags("Health")
    .AllowAnonymous();

app.Run();

// Exposed for integration testing with WebApplicationFactory.
public partial class Program;
