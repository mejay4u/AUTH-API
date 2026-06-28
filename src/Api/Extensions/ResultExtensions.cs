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
            : Problem(result.Error);
    }

    private static IResult Problem(Error error)
    {
        var statusCode = error.Type switch
        {
            ErrorType.Validation => StatusCodes.Status400BadRequest,
            ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
            ErrorType.Forbidden => StatusCodes.Status403Forbidden,
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status500InternalServerError
        };

        return Results.Problem(
            title: error.Code,
            detail: error.Description,
            statusCode: statusCode);
    }
}
