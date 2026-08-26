using AuthApi.Application.Common.Interfaces;
using AuthApi.Application.Common.Models;
using AuthApi.Domain.Common;
using MediatR;
using Microsoft.Extensions.Logging;

namespace AuthApi.Application.Authentication.PassThrough;

/// <summary>
/// The pass-through login use case. It is short on purpose: hand the call to the gateway, log the
/// outcome, return it. Every authentication decision — credential check, lockout, LOB entitlement,
/// token issuance — stays in ARTS, which is the only component that can still reach the member DB.
/// </summary>
public sealed class ForwardLoginCommandHandler(
    IUpstreamAuthGateway gateway,
    ILogger<ForwardLoginCommandHandler> logger)
    : IRequestHandler<ForwardLoginCommand, Result<UpstreamAuthResponse>>
{
    public async Task<Result<UpstreamAuthResponse>> Handle(ForwardLoginCommand request, CancellationToken cancellationToken)
    {
        var result = await gateway.LoginAsync(request.Request, cancellationToken);

        if (result.IsFailure)
        {
            logger.LogError(
                "Login relay failed for LOB {Lob} ({CorrelationId}): {ErrorCode}.",
                request.Lob, request.Request.CorrelationId, result.Error.Code);

            return result;
        }

        // Upstream answered. Even a 401 is a successful relay — we record the status, never the body,
        // because the body carries tokens on the happy path.
        logger.LogInformation(
            "Login relayed for LOB {Lob} ({CorrelationId}): upstream responded {StatusCode}.",
            request.Lob, request.Request.CorrelationId, result.Value.StatusCode);

        return result;
    }
}
