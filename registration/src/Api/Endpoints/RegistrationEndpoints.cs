using MediatR;
using Microsoft.Extensions.Options;
using Registration.Api.Contracts;
using Registration.Api.Extensions;
using Registration.Api.Infrastructure;
using Registration.Application.Registration.CompleteRegistration;
using Registration.Application.Registration.InitiateRegistration;
using Registration.Application.Registration.SetPassword;

namespace Registration.Api.Endpoints;

/// <summary>
/// The three calls Descope's registration flow makes into this service. Paths match the sequence
/// diagram; the phase-3 call is given its own route rather than reusing
/// <c>initiateRegistration</c> with a different body, which is what the diagram literally shows.
/// </summary>
public static class RegistrationEndpoints
{
    public static IEndpointRouteBuilder MapRegistrationEndpoints(this IEndpointRouteBuilder app)
    {
        var connectorAuth = app.ServiceProvider.GetRequiredService<IOptions<ConnectorAuthOptions>>().Value;

        var group = app.MapGroup("/api")
            .WithTags("Registration")
            .RequireRateLimiting(RateLimiterPolicies.Registration)
            .AddEndpointFilter(new ConnectorAuthEndpointFilter(connectorAuth))
            .AddEndpointFilter(new FeatureGateEndpointFilter(FeatureFlags.Registration));

        group.MapPost("/initiateRegistration", InitiateAsync)
            .WithName("InitiateRegistration")
            .WithSummary("Create the pending member record once Descope has verified the email.")
            .Produces<InitiateRegistrationResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPost("/registration/password", SetPasswordAsync)
            .WithName("SetRegistrationPassword")
            .WithSummary("Store the member's password against the pending record (hashed).")
            .Produces<MessageResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        group.MapPost("/completeRegistration", CompleteAsync)
            .WithName("CompleteRegistration")
            .WithSummary("Confirm eligibility against Facets and create the portal user.")
            .Produces<CompleteRegistrationResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status409Conflict);

        return app;
    }

    private static async Task<IResult> InitiateAsync(
        InitiateRegistrationRequest request, ISender sender, CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new InitiateRegistrationCommand(
                request.Email,
                request.FirstName,
                request.LastName,
                request.DateOfBirth,
                request.ZipCode),
            cancellationToken);

        return result.ToHttpResult(r => Results.Ok(InitiateRegistrationResponse.From(r)));
    }

    private static async Task<IResult> SetPasswordAsync(
        SetPasswordRequest request, ISender sender, CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new SetPasswordCommand(request.UserId, request.Password, request.ConfirmPassword),
            cancellationToken);

        return result.ToHttpResult(Results.Ok(new MessageResponse("Password saved.")));
    }

    private static async Task<IResult> CompleteAsync(
        CompleteRegistrationRequest request, ISender sender, CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new CompleteRegistrationCommand(request.Email, request.Ssn, request.MemberId),
            cancellationToken);

        return result.ToHttpResult(r => Results.Ok(CompleteRegistrationResponse.From(r)));
    }
}
