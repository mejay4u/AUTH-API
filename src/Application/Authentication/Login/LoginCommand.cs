using AuthApi.Application.Common.Models;
using AuthApi.Domain.Common;
using MediatR;

namespace AuthApi.Application.Authentication.Login;

/// <summary>
/// Authenticates a member with username + password and, on success, issues a JWT access token
/// (carrying LOBs and Plan IDs) plus a rotating refresh token.
/// <paramref name="IpAddress"/> is captured for refresh-token auditing, not supplied by the caller.
/// </summary>
public sealed record LoginCommand(string Username, string Password, string Lob, string? IpAddress)
    : IRequest<Result<AuthenticationResult>>;
