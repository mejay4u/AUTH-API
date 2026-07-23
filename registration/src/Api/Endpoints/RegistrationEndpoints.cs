using MediatR;
using Registration.Api.Contracts;
using Registration.Api.Extensions;
using Registration.Api.Infrastructure;
using Registration.Application.Registration.CreateAccount;
using Registration.Application.Registration.SendEmailOtp;
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

        // Send and resend are the same operation: the cooldown and per-email request cap are enforced
        // server-side by the OTP service.
        group.MapPost("/otp/send", SendOtpAsync)
            .WithName("SendEmailOtp")
            .WithSummary("Send a verification code to the email address.")
            .Produces<MessageResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status429TooManyRequests);

        group.MapPost("/otp/resend", SendOtpAsync)
            .WithName("ResendEmailOtp")
            .WithSummary("Resend the verification code (subject to the cooldown and per-email cap).")
            .Produces<MessageResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status429TooManyRequests);

        group.MapPost("/otp/verify", VerifyOtpAsync)
            .WithName("VerifyEmailOtp")
            .WithSummary("Verify the emailed code.")
            .Produces<MessageResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status429TooManyRequests);

        group.MapPost("/account", CreateAccountAsync)
            .WithName("CreateAccount")
            .WithSummary("Create the portal user (email is the username).")
            .Produces<CreateAccountResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status409Conflict);

        return app;
    }

    private static async Task<IResult> SendOtpAsync(
        SendOtpRequest request, ISender sender, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new SendEmailOtpCommand(request.Email), cancellationToken);
        return result.ToHttpResult(Results.Ok(new MessageResponse("If the details are valid, a verification code has been sent.")));
    }

    private static async Task<IResult> VerifyOtpAsync(
        VerifyOtpRequest request, ISender sender, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new VerifyEmailOtpCommand(request.Email, request.Code), cancellationToken);
        return result.ToHttpResult(Results.Ok(new MessageResponse("Email verified.")));
    }

    private static async Task<IResult> CreateAccountAsync(
        CreateAccountRequest request, ISender sender, CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new CreateAccountCommand(request.Email, request.Password, request.ConfirmPassword),
            cancellationToken);

        return result.ToHttpResult(r =>
            Results.Created($"/api/v1/registration/account/{r.UserId}", CreateAccountResponse.From(r)));
    }
}
