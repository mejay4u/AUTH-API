using MediatR;
using Microsoft.Extensions.Options;
using Registration.Api.Contracts;
using Registration.Api.Extensions;
using Registration.Api.Infrastructure;
using Registration.Application.Registration.CreateAccount;
using Registration.Application.Registration.InitiateRegistration;

namespace Registration.Api.Endpoints;

/// <summary>
/// The two calls Descope's registration flow makes into this service through its HTTP connectors.
/// The caller is the Descope engine, server to server — there is no member token, so both are
/// authenticated with the connector key instead.
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
            .WithSummary("Store the reviewed registration details and return the pending record's id.")
            .Produces<InitiateRegistrationResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPost("/registration/password", CreateAccountAsync)
            .WithName("CreateAccount")
            .WithSummary("Set the password and create the portal user (email is the username).")
            .Produces<CreateAccountResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status409Conflict);

        return app;
    }

    private static async Task<IResult> InitiateAsync(
        InitiateRegistrationRequest request, ISender sender, CancellationToken cancellationToken)
    {
        // No email cross-check and no Descope user id to record: the caller is the flow, not the
        // member, and the flow creates its shadow record only AFTER this call returns. Until a later
        // step populates it, the Descope-to-member link is the email address.
        var result = await sender.Send(
            new InitiateRegistrationCommand(
                request.Email,
                request.FirstName,
                request.LastName,
                request.DateOfBirth,
                request.ZipCode,
                request.ContactNumber),
            cancellationToken);

        return result.ToHttpResult(r => Results.Ok(InitiateRegistrationResponse.From(r)));
    }

    private static async Task<IResult> CreateAccountAsync(
        CreateAccountRequest request, ISender sender, CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new CreateAccountCommand(request.UserId, request.Password, request.ConfirmPassword),
            cancellationToken);

        return result.ToHttpResult(r =>
            Results.Created($"/api/registration/account/{r.UserId}", CreateAccountResponse.From(r)));
    }
}
