using AuthApi.Application.Common.Models;
using AuthApi.Domain.Common;

namespace AuthApi.Application.Common.Interfaces;

/// <summary>
/// The outbound port for the on-prem ARTS auth service. The Application layer knows only that "some
/// gateway answers an auth call"; that it happens over HTTP, through Apigee Internal, with an API key
/// and mTLS, is entirely an Infrastructure concern.
///
/// The <see cref="Result{T}"/> is a failure only when the BFA could not get an answer (unreachable,
/// timed out). An answer that happens to be a rejection — 401, 403, 423 — is a SUCCESS at this
/// boundary, carried inside <see cref="UpstreamAuthResponse.StatusCode"/>, because relaying it is
/// exactly the job.
/// </summary>
public interface IUpstreamAuthGateway
{
    /// <summary>Relays a login call to the upstream auth endpoint.</summary>
    Task<Result<UpstreamAuthResponse>> LoginAsync(UpstreamAuthRequest request, CancellationToken cancellationToken);

    /// <summary>Relays a refresh-token call to the upstream refresh endpoint.</summary>
    Task<Result<UpstreamAuthResponse>> RefreshAsync(UpstreamAuthRequest request, CancellationToken cancellationToken);
}
