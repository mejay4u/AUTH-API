using AuthApi.Application.Common.Models;
using AuthApi.Domain.Common;
using MediatR;

namespace AuthApi.Application.Authentication.PassThrough;

/// <summary>
/// Relays a login call to the on-prem ARTS auth service. The pass-through counterpart of
/// <see cref="Login.LoginCommand"/>: same use case, but the decision is ARTS's, not ours.
///
/// Note what this command does NOT carry: the password. The credential lives only inside the opaque
/// <see cref="UpstreamAuthRequest.Body"/> that gets forwarded, so no handler, validator, log scope or
/// exception dump can accidentally surface it. <paramref name="HasPassword"/> is the only thing the
/// validator needs, and it is a bool.
/// </summary>
/// <param name="Request">The verbatim call to forward upstream.</param>
/// <param name="Username">Parsed from the body for validation and log correlation only.</param>
/// <param name="Lob">Parsed line-of-business selector, forwarded inside the body.</param>
/// <param name="HasPassword">Whether a non-empty password was present in the body.</param>
public sealed record ForwardLoginCommand(
    UpstreamAuthRequest Request,
    string? Username,
    string? Lob,
    bool HasPassword)
    : IRequest<Result<UpstreamAuthResponse>>;
