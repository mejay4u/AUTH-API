using AuthApi.Application.Common.Interfaces;
using AuthApi.Application.Common.Models;
using AuthApi.Application.Common.Security;
using AuthApi.Domain.Common;
using AuthApi.Domain.Members;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using DomainRefreshToken = AuthApi.Domain.Members.RefreshToken;

namespace AuthApi.Application.Authentication.Login;

public sealed class LoginCommandHandler(
    IApplicationDbContext db,
    IPasswordHasher passwordHasher,
    ITokenService tokenService,
    IDateTimeProvider clock,
    IOptions<AccountLockoutOptions> lockoutOptions,
    ILogger<LoginCommandHandler> logger)
    : IRequestHandler<LoginCommand, Result<AuthenticationResult>>
{
    private readonly AccountLockoutOptions _lockout = lockoutOptions.Value;

    public async Task<Result<AuthenticationResult>> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var now = clock.UtcNow;

        var member = await db.Members
            .Include(m => m.MemberLobs).ThenInclude(ml => ml.Lob)
            .Include(m => m.MemberPlans)
            .FirstOrDefaultAsync(m => m.Username == request.Username, cancellationToken);

        // Generic failure for unknown user — avoids user enumeration. We still run a hash verification
        // against a dummy value so response timing does not reveal whether the username exists.
        if (member is null)
        {
            passwordHasher.Verify(request.Password, DummyHash, DummySalt);
            logger.LogInformation("Login failed: unknown username.");
            return AuthErrors.InvalidCredentials;
        }

        if (member.IsLockedOut(now))
        {
            logger.LogWarning("Login blocked: account {MemberId} is locked until {LockoutEnd}.", member.Id, member.LockoutEndUtc);
            return AuthErrors.AccountLocked;
        }

        if (!member.IsActive || !passwordHasher.Verify(request.Password, member.PasswordHash, member.PasswordSalt))
        {
            member.RegisterFailedLogin(_lockout.MaxFailedAttempts, _lockout.LockoutDuration, now);
            await db.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Login failed for member {MemberId}.", member.Id);
            return AuthErrors.InvalidCredentials;
        }

        member.RegisterSuccessfulLogin(now);

        var lobCodes = member.LobCodes();
        var planIds = member.PlanIds();

        var accessToken = tokenService.CreateAccessToken(member, lobCodes, planIds);
        var refreshToken = tokenService.CreateRefreshToken();

        db.RefreshTokens.Add(new DomainRefreshToken
        {
            Id = Guid.NewGuid(),
            MemberId = member.Id,
            TokenHash = refreshToken.Hash,
            CreatedUtc = now,
            CreatedByIp = request.IpAddress,
            ExpiresUtc = refreshToken.ExpiresUtc
        });

        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Member {MemberId} authenticated successfully.", member.Id);

        return new AuthenticationResult(
            member.Id,
            member.Username,
            accessToken.Value,
            accessToken.ExpiresUtc,
            refreshToken.Raw,
            refreshToken.ExpiresUtc,
            lobCodes,
            planIds);
    }

    // A well-formed but invalid hash/salt so the unknown-user path performs equivalent work.
    private const string DummyHash = "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA=";
    private const string DummySalt = "AAAAAAAAAAAAAAAAAAAAAA==";
}
