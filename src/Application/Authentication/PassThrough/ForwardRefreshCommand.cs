using AuthApi.Application.Common.Models;
using AuthApi.Domain.Common;
using MediatR;

namespace AuthApi.Application.Authentication.PassThrough;

/// <summary>
/// Relays a refresh-token exchange to the on-prem ARTS auth service. As with
/// <see cref="ForwardLoginCommand"/>, the token itself never leaves the opaque forwarded body —
/// a refresh token is a credential and is treated like one.
/// </summary>
/// <param name="Request">The verbatim call to forward upstream.</param>
/// <param name="HasRefreshToken">Whether a non-empty refresh token was present in the body.</param>
public sealed record ForwardRefreshCommand(
    UpstreamAuthRequest Request,
    bool HasRefreshToken)
    : IRequest<Result<UpstreamAuthResponse>>;
