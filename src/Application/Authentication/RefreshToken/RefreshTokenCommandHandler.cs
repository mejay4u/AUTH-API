using AuthApi.Application.Common.Interfaces;
using AuthApi.Application.Common.Models;
using AuthApi.Domain.Common;
using AuthApi.Domain.Members;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using DomainRefreshToken = AuthApi.Domain.Members.RefreshToken;

namespace AuthApi.Application.Authentication.RefreshToken;

public sealed class RefreshTokenCommandHandler(
    IApplicationDbContext db,
    ITokenService tokenService,
    IDateTimeProvider clock,
    ILogger<RefreshTokenCommandHandler> logger)
    : IRequestHandler<RefreshTokenCommand, Result<AuthenticationResult>>
{
    public async Task<Result<AuthenticationResult>> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var now = clock.UtcNow;
        var presentedHash = tokenService.HashRefreshToken(request.RefreshToken);

        var existing = await db.RefreshTokens
            .Include(rt => rt.Member).ThenInclude(m => m.MemberLobs).ThenInclude(ml => ml.Lob)
            .Include(rt => rt.Member).ThenInclude(m => m.MemberPlans)
            .FirstOrDefaultAsync(rt => rt.TokenHash == presentedHash, cancellationToken);

        if (existing is null)
        {
            logger.LogWarning("Refresh failed: token not recognised.");
            return AuthErrors.InvalidRefreshToken;
        }

        // Reuse detection: a token that exists but is already revoked/expired is being replayed.
        // Revoke every active token for the member to contain a possible theft.
        if (!existing.IsActive(now))
        {
            await RevokeAllActiveTokensAsync(existing.MemberId, now, request.IpAddress, cancellationToken);
            logger.LogWarning("Refresh token reuse detected for member {MemberId}; token chain revoked.", existing.MemberId);
            return AuthErrors.InvalidRefreshToken;
        }

        var member = existing.Member;
        if (!member.IsActive)
        {
            return AuthErrors.InvalidRefreshToken;
        }

        var lobCodes = member.LobCodes();
        var planIds = member.PlanIds();

        var newRefresh = tokenService.CreateRefreshToken();
        existing.Revoke(now, request.IpAddress, newRefresh.Hash);

        db.RefreshTokens.Add(new DomainRefreshToken
        {
            Id = Guid.NewGuid(),
            MemberId = member.Id,
            TokenHash = newRefresh.Hash,
            CreatedUtc = now,
            CreatedByIp = request.IpAddress,
            ExpiresUtc = newRefresh.ExpiresUtc
        });

        var accessToken = tokenService.CreateAccessToken(member, lobCodes, planIds);

        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Refresh token rotated for member {MemberId}.", member.Id);

        return new AuthenticationResult(
            member.Id,
            member.Username,
            accessToken.Value,
            accessToken.ExpiresUtc,
            newRefresh.Raw,
            newRefresh.ExpiresUtc,
            lobCodes,
            planIds);
    }

    private async Task RevokeAllActiveTokensAsync(Guid memberId, DateTime now, string? ip, CancellationToken ct)
    {
        var active = await db.RefreshTokens
            .Where(rt => rt.MemberId == memberId && rt.RevokedUtc == null && rt.ExpiresUtc > now)
            .ToListAsync(ct);

        foreach (var token in active)
        {
            token.Revoke(now, ip);
        }

        await db.SaveChangesAsync(ct);
    }
}
