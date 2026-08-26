using AuthApi.Application.Common.Interfaces;
using AuthApi.Application.Common.Models;
using AuthApi.Domain.Common;
using MediatR;
using Microsoft.Extensions.Logging;

namespace AuthApi.Application.Authentication.PassThrough;

/// <summary>The pass-through refresh use case — rotation and revocation stay with ARTS.</summary>
public sealed class ForwardRefreshCommandHandler(
    IUpstreamAuthGateway gateway,
    ILogger<ForwardRefreshCommandHandler> logger)
    : IRequestHandler<ForwardRefreshCommand, Result<UpstreamAuthResponse>>
{
    public async Task<Result<UpstreamAuthResponse>> Handle(ForwardRefreshCommand request, CancellationToken cancellationToken)
    {
        var result = await gateway.RefreshAsync(request.Request, cancellationToken);

        if (result.IsFailure)
        {
            logger.LogError(
                "Refresh relay failed ({CorrelationId}): {ErrorCode}.",
                request.Request.CorrelationId, result.Error.Code);

            return result;
        }

        logger.LogInformation(
            "Refresh relayed ({CorrelationId}): upstream responded {StatusCode}.",
            request.Request.CorrelationId, result.Value.StatusCode);

        return result;
    }
}
