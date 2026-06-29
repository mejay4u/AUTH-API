using AuthApi.Application.Common.Interfaces;
using AuthApi.Application.Common.Models;
using AuthApi.Domain.Common;
using AuthApi.Domain.Members;
using MediatR;
using Microsoft.Extensions.Logging;
using DomainRefreshToken = AuthApi.Domain.Members.RefreshToken;

namespace AuthApi.Application.Authentication.Login;

/// <summary>
/// The login use case — the clean-architecture replacement for the legacy
/// <c>AccountService.Validate3FLogin</c>. There is no separate "service": this handler IS the
/// application service. It fetches login data through <see cref="IAccountRepository"/> (the equivalent
/// of <c>AccountRepository.GetMemberPortalLoginData</c>), verifies the password, then issues tokens.
/// </summary>
public sealed class LoginCommandHandler(
    IAccountRepository accountRepository,
    IApplicationDbContext db,
    IPasswordHasher passwordHasher,
    ITokenService tokenService,
    IDateTimeProvider clock,
    ILogger<LoginCommandHandler> logger)
    : IRequestHandler<LoginCommand, Result<AuthenticationResult>>
{
    public async Task<Result<AuthenticationResult>> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var now = clock.UtcNow;

        var data = await accountRepository.GetMemberPortalLoginDataAsync(request.Username, request.Lob, cancellationToken);

        // Unknown user: still run a hash verification against a dummy value so response timing does not
        // reveal whether the username exists (prevents user enumeration via timing).
        if (data is null)
        {
            passwordHasher.Verify(request.Password, DummyHash, DummySalt);
            logger.LogInformation("Login failed: unknown username.");
            return AuthErrors.InvalidCredentials;
        }

        // Inactive account and wrong password return the SAME generic error (no enumeration).
        if (!data.IsActive || !passwordHasher.Verify(request.Password, data.PasswordHash, data.PasswordSalt))
        {
            logger.LogInformation("Login failed for member {MemberId}.", data.MemberId);
            return AuthErrors.InvalidCredentials;
        }

        var accessToken = tokenService.CreateAccessToken(data);
        var refreshToken = tokenService.CreateRefreshToken();

        db.RefreshTokens.Add(new DomainRefreshToken
        {
            Id = Guid.NewGuid(),
            MemberId = data.MemberId,
            Lob = request.Lob,
            TokenHash = refreshToken.Hash,
            CreatedUtc = now,
            CreatedByIp = request.IpAddress,
            ExpiresUtc = refreshToken.ExpiresUtc
        });

        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Member {MemberId} authenticated successfully.", data.MemberId);

        return new AuthenticationResult(
            data.MemberId,
            data.Username,
            accessToken.Value,
            accessToken.ExpiresUtc,
            refreshToken.Raw,
            refreshToken.ExpiresUtc,
            data.Lobs,
            data.PlanIds);
    }

    // A well-formed but invalid hash/salt so the unknown-user path performs equivalent work.
    private const string DummyHash = "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA=";
    private const string DummySalt = "AAAAAAAAAAAAAAAAAAAAAA==";
}
