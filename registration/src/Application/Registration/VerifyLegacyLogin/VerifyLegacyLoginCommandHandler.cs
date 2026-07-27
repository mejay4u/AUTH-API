using MediatR;
using Microsoft.Extensions.Logging;
using Registration.Application.Common;
using Registration.Application.Common.Interfaces;
using Registration.Application.Common.Models;
using Registration.Domain.Common;
using Registration.Domain.Migration;
using Registration.Domain.Registration;
using Registration.Domain.Users;

namespace Registration.Application.Registration.VerifyLegacyLogin;

/// <summary>
/// Verifies a legacy member on first login (Descope-driven JIT migration). If the user is already known
/// to us, returns their profile. Otherwise verifies via <see cref="ILegacyMemberVerifier"/> and, on
/// success, creates a migrated <c>User</c> (no password — Descope owns it) and writes an audit record.
/// </summary>
public sealed class VerifyLegacyLoginCommandHandler(
    IUserProfileRepository users,
    IMigrationAuditRepository audit,
    ILegacyMemberVerifier legacyVerifier,
    TimeProvider timeProvider,
    ILogger<VerifyLegacyLoginCommandHandler> logger)
    : IRequestHandler<VerifyLegacyLoginCommand, Result<MemberProfile>>
{
    public async Task<Result<MemberProfile>> Handle(VerifyLegacyLoginCommand request, CancellationToken cancellationToken)
    {
        var email = EmailNormalizer.Normalize(request.Email);
        var now = timeProvider.GetUtcNow().UtcDateTime;

        // Already migrated/registered — let Descope proceed without touching the legacy system.
        var existing = await users.GetByEmailAsync(email, cancellationToken);
        if (existing is not null)
        {
            return new MemberProfile(existing.Id, existing.Email, existing.FirstName, existing.LastName, existing.Origin);
        }

        var verification = await legacyVerifier.VerifyAsync(email, request.Password, cancellationToken);
        if (!verification.Verified || verification.Profile is null)
        {
            await audit.AddAsync(Audit(email, null, false, "legacy verification failed", request.SourceIp, now), cancellationToken);
            logger.LogInformation("JIT migration: legacy verification failed.");
            return RegistrationErrors.LegacyVerificationFailed;
        }

        var profile = verification.Profile;
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            Username = email,
            FirstName = profile.FirstName,
            LastName = profile.LastName,
            DateOfBirth = profile.DateOfBirth,
            ZipCode = profile.ZipCode,
            ContactNumber = profile.ContactNumber,
            DescopeUserId = null, // Descope links/assigns it later via the sync webhook
            Origin = UserOrigin.Migrated,
            LegacyMemberId = profile.LegacyMemberId,
            IsActive = true,
            CreatedUtc = now,
            UpdatedUtc = now,
            MigratedUtc = now
        };

        await users.UpsertAsync(user, cancellationToken);
        await audit.AddAsync(Audit(email, profile.LegacyMemberId, true, "migrated", request.SourceIp, now), cancellationToken);

        logger.LogInformation("JIT migration: created migrated user {UserId}.", user.Id);
        return new MemberProfile(user.Id, user.Email, user.FirstName, user.LastName, user.Origin);
    }

    private static MigrationAudit Audit(string email, string? legacyMemberId, bool success, string detail, string? sourceIp, DateTime now) => new()
    {
        Id = Guid.NewGuid(),
        Email = email,
        LegacyMemberId = legacyMemberId,
        Success = success,
        Detail = detail,
        SourceIp = sourceIp,
        OccurredUtc = now
    };
}
