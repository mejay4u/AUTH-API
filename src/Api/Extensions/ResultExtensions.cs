using AuthApi.Api.PassThrough;
using AuthApi.Application.Common.Models;
using AuthApi.Domain.Common;

namespace AuthApi.Api.Extensions;

/// <summary>
/// Maps the Application/Domain <see cref="Result{T}"/> onto HTTP responses without leaking framework
/// types into the lower layers. Failures become RFC 7807 ProblemDetails with a status derived from
/// the error type.
/// </summary>
public static class ResultExtensions
{
    public static IResult ToHttpResult<TValue, TResponse>(
        this Result<TValue> result,
        Func<TValue, TResponse> onSuccess)
    {
        return result.IsSuccess
            ? Results.Ok(onSuccess(result.Value))
            : result.Error.ToProblem();
    }

    /// <summary>
    /// Completes a pass-through call.
    ///
    /// The asymmetry here is the whole point of the relay: on success we replay upstream's response
    /// untouched — its status code, its body, its allow-listed headers — instead of re-wrapping it in
    /// a shape of our own. Only a failure, meaning the BFA never got an answer, produces a
    /// ProblemDetails that this API authored.
    /// </summary>
    public static IResult ToPassThroughResult(this Result<UpstreamAuthResponse> result)
    {
        return result.IsSuccess
            ? new UpstreamRelayResult(result.Value)
            : result.Error.ToProblem();
    }

    /// <summary>Renders a domain <see cref="Error"/> as RFC 7807 ProblemDetails.</summary>
    public static IResult ToProblem(this Error error)
    {
        var statusCode = error.Type switch
        {
            ErrorType.Validation => StatusCodes.Status400BadRequest,
            ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
            ErrorType.Forbidden => StatusCodes.Status403Forbidden,
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            ErrorType.Unavailable => StatusCodes.Status503ServiceUnavailable,
            ErrorType.Timeout => StatusCodes.Status504GatewayTimeout,
            _ => StatusCodes.Status500InternalServerError
        };

        return Results.Problem(
            title: error.Code,
            detail: error.Description,
            statusCode: statusCode);
    }
}
