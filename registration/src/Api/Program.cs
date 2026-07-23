using Registration.Api;
using Registration.Api.Endpoints;
using Registration.Api.Infrastructure;
using Registration.Application;
using Registration.Infrastructure;
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

app.UseCors(Registration.Api.DependencyInjection.CorsPolicyName);

app.UseRateLimiter();

app.MapRegistrationEndpoints();

app.MapGet("/health", () => Results.Ok(new { status = "healthy" }))
    .WithTags("Health");

app.Run();

// Exposed for integration testing with WebApplicationFactory.
public partial class Program;
