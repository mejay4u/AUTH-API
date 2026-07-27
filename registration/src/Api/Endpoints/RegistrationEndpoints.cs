using MediatR;
using Registration.Api.Contracts;
using Registration.Api.Extensions;
using Registration.Api.Infrastructure;
using Registration.Application.Registration.SyncDescopeUser;
using Registration.Application.Registration.VerifyLegacyLogin;

namespace Registration.Api.Endpoints;

public static class RegistrationEndpoints
{
    public static IEndpointRouteBuilder MapRegistrationEndpoints(this IEndpointRouteBuilder app)
    {
        // Descope calls these machine-to-machine; they are HMAC-signature verified by
        // DescopeSignatureMiddleware and gated by the Registration feature flag.
        var group = app.MapGroup("/api/v1/registration/descope")
            .WithTags("Descope Integration")
            .RequireRateLimiting(RateLimiterPolicies.Registration)
            .AddEndpointFilter(new FeatureGateEndpointFilter(FeatureFlags.Registration));

        group.MapPost("/users", SyncUserAsync)
            .WithName("SyncDescopeUser")
            .WithSummary("Upsert a user Descope pushed to us (idempotent).")
            .Produces<MessageResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        group.MapPost("/verify", VerifyAsync)
            .WithName("VerifyLegacyLogin")
            .WithSummary("JIT verify a legacy member's credentials on first login (Descope migration hook).")
            .Produces<DescopeVerifyResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        return app;
    }

    private static async Task<IResult> SyncUserAsync(
        DescopeUserSyncRequest request, ISender sender, CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new SyncDescopeUserCommand(
                request.DescopeUserId,
                request.Email,
                request.FirstName,
                request.LastName,
                request.DateOfBirth,
                request.ZipCode,
                request.ContactNumber),
            cancellationToken);

        return result.ToHttpResult(Results.Ok(new MessageResponse("User synced.")));
    }

    private static async Task<IResult> VerifyAsync(
        DescopeVerifyRequest request, ISender sender, HttpContext httpContext, CancellationToken cancellationToken)
    {
        var ip = httpContext.Connection.RemoteIpAddress?.ToString();
        var result = await sender.Send(new VerifyLegacyLoginCommand(request.Email, request.Password, ip), cancellationToken);
        return result.ToHttpResult(profile => Results.Ok(DescopeVerifyResponse.From(profile)));
    }
}
