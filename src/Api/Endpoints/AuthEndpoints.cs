using AuthApi.Api.Contracts;
using AuthApi.Api.Extensions;
using AuthApi.Application.Authentication.Login;
using AuthApi.Application.Authentication.RefreshToken;
using MediatR;

namespace AuthApi.Api.Endpoints;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/auth")
            .WithTags("Authentication")
            .RequireRateLimiting(RateLimiterPolicies.Auth);

        group.MapPost("/login", LoginAsync)
            .WithName("Login")
            .WithSummary("Authenticate a member and issue JWT access + refresh tokens.")
            .AllowAnonymous()
            .Produces<AuthResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);

        group.MapPost("/refresh", RefreshAsync)
            .WithName("Refresh")
            .WithSummary("Exchange a valid refresh token for a new access token (with rotation).")
            .AllowAnonymous()
            .Produces<AuthResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        return app;
    }

    private static async Task<IResult> LoginAsync(
        LoginRequest request, ISender sender, HttpContext httpContext, CancellationToken cancellationToken)
    {
        var ip = httpContext.Connection.RemoteIpAddress?.ToString();
        var result = await sender.Send(new LoginCommand(request.Username, request.Password, request.Lob, ip), cancellationToken);
        return result.ToHttpResult(AuthResponse.From);
    }

    private static async Task<IResult> RefreshAsync(
        RefreshRequest request, ISender sender, HttpContext httpContext, CancellationToken cancellationToken)
    {
        var ip = httpContext.Connection.RemoteIpAddress?.ToString();
        var result = await sender.Send(new RefreshTokenCommand(request.RefreshToken, ip), cancellationToken);
        return result.ToHttpResult(AuthResponse.From);
    }
}
