using MediatR;
using Registration.Api.Contracts;
using Registration.Api.Extensions;
using Registration.Api.Infrastructure;
using Registration.Application.Registration.CreateAccount;
using Registration.Application.Registration.ResendOtp;
using Registration.Application.Registration.StartRegistration;
using Registration.Application.Registration.VerifyEmailOtp;

namespace Registration.Api.Endpoints;

public static class RegistrationEndpoints
{
    public static IEndpointRouteBuilder MapRegistrationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/registration")
            .WithTags("Registration")
            .RequireRateLimiting(RateLimiterPolicies.Registration)
            .AddEndpointFilter(new FeatureGateEndpointFilter(FeatureFlags.Registration));

        group.MapPost("/start", StartAsync)
            .WithName("StartRegistration")
            .WithSummary("Open a registration session with the personal information and email a code.")
            .Produces<StartRegistrationResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        group.MapPost("/otp/resend", ResendAsync)
            .WithName("ResendEmailOtp")
            .WithSummary("Resend the verification code (subject to the cooldown and per-email cap).")
            .Produces<MessageResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status429TooManyRequests);

        group.MapPost("/otp/verify", VerifyAsync)
            .WithName("VerifyEmailOtp")
            .WithSummary("Verify the emailed code for a registration session.")
            .Produces<MessageResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status429TooManyRequests);

        group.MapPost("/account", CreateAccountAsync)
            .WithName("CreateAccount")
            .WithSummary("Create the portal user from a verified session (email is the username).")
            .Produces<CreateAccountResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status409Conflict);

        return app;
    }

    private static async Task<IResult> StartAsync(
        StartRegistrationRequest request, ISender sender, CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new StartRegistrationCommand(
                request.FirstName,
                request.LastName,
                request.DateOfBirth,
                request.ZipCode,
                request.Email,
                request.ContactNumber),
            cancellationToken);

        return result.ToHttpResult(r => Results.Ok(StartRegistrationResponse.From(r)));
    }

    private static async Task<IResult> ResendAsync(
        ResendOtpRequest request, ISender sender, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new ResendOtpCommand(request.RegistrationId), cancellationToken);
        return result.ToHttpResult(Results.Ok(new MessageResponse("If the session is valid, a verification code has been sent.")));
    }

    private static async Task<IResult> VerifyAsync(
        VerifyOtpRequest request, ISender sender, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new VerifyEmailOtpCommand(request.RegistrationId, request.Code), cancellationToken);
        return result.ToHttpResult(Results.Ok(new MessageResponse("Email verified.")));
    }

    private static async Task<IResult> CreateAccountAsync(
        CreateAccountRequest request, ISender sender, CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new CreateAccountCommand(request.RegistrationId, request.Password, request.ConfirmPassword),
            cancellationToken);

        return result.ToHttpResult(r =>
            Results.Created($"/api/v1/registration/account/{r.UserId}", CreateAccountResponse.From(r)));
    }
}
