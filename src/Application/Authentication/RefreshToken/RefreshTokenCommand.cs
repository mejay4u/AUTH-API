using AuthApi.Application.Common.Models;
using AuthApi.Domain.Common;
using MediatR;

namespace AuthApi.Application.Authentication.RefreshToken;

/// <summary>
/// Exchanges a valid, unexpired refresh token for a new access token and a new (rotated) refresh
/// token. The presented token is single-use; reusing a rotated token revokes the member's token
/// chain as a compromise-detection measure.
/// </summary>
public sealed record RefreshTokenCommand(string RefreshToken, string? IpAddress)
    : IRequest<Result<AuthenticationResult>>;
