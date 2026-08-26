using AuthApi.Api.Contracts;
using AuthApi.Api.Extensions;
using AuthApi.Api.PassThrough;
using AuthApi.Application.Authentication.Login;
using AuthApi.Application.Authentication.PassThrough;
using AuthApi.Application.Authentication.RefreshToken;
using AuthApi.Application.Common.Configuration;
using AuthApi.Application.Common.Models;
using AuthApi.Domain.Common;
using AuthApi.Domain.Members;
using MediatR;
using Microsoft.Extensions.Options;

namespace AuthApi.Api.Endpoints;

/// <summary>
/// The <c>/api/v1/auth</c> surface. The route table is identical in both modes — the Member Portal
/// cannot tell whether it is talking to a self-contained Auth API or to the BFA relaying to on-prem
/// ARTS — but the handlers behind it differ. See <see cref="AuthMode"/>.
/// </summary>
public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var mode = app.ServiceProvider.GetRequiredService<IOptions<AuthOptions>>().Value.Mode;

        var group = app.MapGroup("/api/v1/auth")
            .WithTags("Authentication")
            .RequireRateLimiting(RateLimiterPolicies.Auth);

        return mode == AuthMode.PassThrough
            ? MapPassThroughEndpoints(app, group)
            : MapLocalEndpoints(app, group);
    }

    private static IEndpointRouteBuilder MapLocalEndpoints(IEndpointRouteBuilder app, RouteGroupBuilder group)
    {
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

    private static IEndpointRouteBuilder MapPassThroughEndpoints(IEndpointRouteBuilder app, RouteGroupBuilder group)
    {
        group.MapPost("/login", RelayLoginAsync)
            .WithName("Login")
            .WithSummary("Relay a login to the on-prem ARTS auth service via Apigee Internal.")
            .WithDescription(RelayDescription)
            .AllowAnonymous()
            .Accepts<LoginRequest>("application/json")
            .Produces<AuthResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable)
            .ProducesProblem(StatusCodes.Status504GatewayTimeout);

        group.MapPost("/refresh", RelayRefreshAsync)
            .WithName("Refresh")
            .WithSummary("Relay a refresh-token exchange to the on-prem ARTS auth service via Apigee Internal.")
            .WithDescription(RelayDescription)
            .AllowAnonymous()
            .Accepts<RefreshRequest>("application/json")
            .Produces<AuthResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable)
            .ProducesProblem(StatusCodes.Status504GatewayTimeout);

        return app;
    }

    // ---------------------------------------------------------------------------------------------
    // Local mode — this process authenticates and issues its own tokens.
    // ---------------------------------------------------------------------------------------------

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

    // ---------------------------------------------------------------------------------------------
    // Pass-through mode — ARTS authenticates; this process only carries the call.
    //
    // Note these take HttpContext rather than a bound DTO. Model binding would deserialize the body
    // and hand us an object, and re-serializing that object is precisely what a relay must not do:
    // any field ARTS understands but the BFA's DTO lacks would be silently dropped on the way through.
    // ---------------------------------------------------------------------------------------------

    private static Task<IResult> RelayLoginAsync(
        HttpContext httpContext,
        ISender sender,
        IOptions<AuthOptions> authOptions,
        CancellationToken cancellationToken) =>
        RelayAsync(
            httpContext,
            sender,
            authOptions.Value,
            // Only the non-secret fields are lifted out, for validation and logging. The password
            // stays inside the opaque body that gets forwarded, and never reaches the command.
            static (request, fields) => new ForwardLoginCommand(
                request,
                fields.GetValueOrDefault("username"),
                fields.GetValueOrDefault("lob"),
                !string.IsNullOrEmpty(fields.GetValueOrDefault("password"))),
            cancellationToken);

    private static Task<IResult> RelayRefreshAsync(
        HttpContext httpContext,
        ISender sender,
        IOptions<AuthOptions> authOptions,
        CancellationToken cancellationToken) =>
        RelayAsync(
            httpContext,
            sender,
            authOptions.Value,
            static (request, fields) => new ForwardRefreshCommand(
                request,
                !string.IsNullOrEmpty(fields.GetValueOrDefault("refreshToken"))),
            cancellationToken);

    /// <summary>
    /// The shared shape of a relayed call: buffer the body, peek at its fields, hand the use case a
    /// command, replay whatever comes back. Only <paramref name="toCommand"/> differs per endpoint.
    /// </summary>
    private static async Task<IResult> RelayAsync(
        HttpContext httpContext,
        ISender sender,
        AuthOptions authOptions,
        Func<UpstreamAuthRequest, Dictionary<string, string?>, IRequest<Result<UpstreamAuthResponse>>> toCommand,
        CancellationToken cancellationToken)
    {
        var read = await PassThroughRequestReader.ReadAsync(httpContext, authOptions, cancellationToken);

        if (read.IsFailure)
        {
            return read.Error.ToProblem();
        }

        if (!PassThroughRequestReader.TryReadJsonFields(read.Value.Body, out var fields))
        {
            return UpstreamAuthErrors.MalformedPayload.ToProblem();
        }

        var result = await sender.Send(toCommand(read.Value, fields), cancellationToken);

        return result.ToPassThroughResult();
    }

    private const string RelayDescription =
        "This deployment is a pass-through. The request body is forwarded verbatim to the on-prem ARTS " +
        "auth service through Apigee Internal, and ARTS's status code, body and allow-listed headers " +
        "are returned unchanged — so the documented 200 schema is ARTS's contract, not this API's. " +
        "503 and 504 are the only responses this API originates: they mean ARTS could not be reached " +
        "or did not answer in time.";
}
