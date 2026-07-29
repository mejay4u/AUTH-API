using MediatR;
using Microsoft.Extensions.Options;
using Registration.Api.Contracts;
using Registration.Api.Extensions;
using Registration.Api.Infrastructure;
using Registration.Application.Registration.CreateAccount;
using Registration.Application.Registration.InitiateRegistration;
using Registration.Domain.Registration;

namespace Registration.Api.Endpoints;

/// <summary>
/// The two calls the app makes to finish registration, after Descope has verified the email.
/// Both require the Descope session token from that verification.
/// </summary>
public static class RegistrationEndpoints
{
    public static IEndpointRouteBuilder MapRegistrationEndpoints(this IEndpointRouteBuilder app)
    {
        var descope = app.ServiceProvider.GetRequiredService<IOptions<DescopeAuthOptions>>().Value;

        var group = app.MapGroup("/api")
            .WithTags("Registration")
            .RequireRateLimiting(RateLimiterPolicies.Registration)
            .AddEndpointFilter(new FeatureGateEndpointFilter(FeatureFlags.Registration));

        if (!descope.AllowAnonymous)
        {
            group.RequireAuthorization();
        }

        group.MapPost("/initiateRegistration", InitiateAsync)
            .WithName("InitiateRegistration")
            .WithSummary("Store the reviewed registration details and return the pending record's id.")
            .Produces<InitiateRegistrationResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
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
        InitiateRegistrationRequest request,
        HttpContext httpContext,
        ISender sender,
        IOptions<DescopeAuthOptions> descopeOptions,
        CancellationToken cancellationToken)
    {
        // The body says which email is registering; the token says which email was verified. They
        // have to be the same, or a valid token for one address could register another.
        if (!descopeOptions.Value.AllowAnonymous)
        {
            var verifiedEmail = DescopePrincipal.GetEmail(httpContext.User);
            if (verifiedEmail is not null
                && !string.Equals(verifiedEmail, request.Email, StringComparison.OrdinalIgnoreCase))
            {
                return Results.Problem(
                    title: RegistrationErrors.EmailMismatch.Code,
                    detail: RegistrationErrors.EmailMismatch.Description,
                    statusCode: StatusCodes.Status403Forbidden);
            }
        }

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
