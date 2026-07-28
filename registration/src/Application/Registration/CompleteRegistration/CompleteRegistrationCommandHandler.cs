using MediatR;
using Microsoft.Extensions.Logging;
using Registration.Application.Common;
using Registration.Application.Common.Interfaces;
using Registration.Application.Common.Models;
using Registration.Domain.Common;
using Registration.Domain.Registration;

namespace Registration.Application.Registration.CompleteRegistration;

/// <summary>
/// Confirms eligibility and promotes the Pending record into a real portal user.
///
/// Order matters: the record must exist, must already carry a password, and the member must match a
/// Facets record — only then is the user created. Nothing partial is written, so a failed eligibility
/// check leaves the member able to retry against the same pending record.
/// </summary>
public sealed class CompleteRegistrationCommandHandler(
    IPendingRegistrationRepository pendingRepository,
    IUserRegistrationRepository userRepository,
    IFacetsClient facetsClient,
    TimeProvider timeProvider,
    ILogger<CompleteRegistrationCommandHandler> logger)
    : IRequestHandler<CompleteRegistrationCommand, Result<CompleteRegistrationResult>>
{
    public async Task<Result<CompleteRegistrationResult>> Handle(
        CompleteRegistrationCommand request,
        CancellationToken cancellationToken)
    {
        var email = EmailNormalizer.Normalize(request.Email);

        var pending = await pendingRepository.GetByEmailAsync(email, cancellationToken);
        if (pending is null || pending.IsExpired(timeProvider.GetUtcNow()))
        {
            return RegistrationErrors.RegistrationNotFoundOrExpired;
        }

        if (!pending.HasPassword)
        {
            return RegistrationErrors.PasswordNotSet;
        }

        if (await userRepository.EmailExistsAsync(email, cancellationToken))
        {
            return RegistrationErrors.EmailAlreadyRegistered;
        }

        var ssn = NormalizeSsn(request.Ssn);

        var lookup = await facetsClient.FindMemberAsync(
            new FacetsMemberLookup(
                Ssn: ssn,
                MemberId: string.IsNullOrWhiteSpace(request.MemberId) ? null : request.MemberId.Trim(),
                FirstName: pending.FirstName,
                LastName: pending.LastName,
                DateOfBirth: pending.DateOfBirth,
                ZipCode: pending.ZipCode),
            cancellationToken);

        if (lookup.IsFailure)
        {
            // Already the right error type from the client (not-found vs unreachable) — pass it on.
            logger.LogInformation(
                "Eligibility lookup failed for pending registration {UserId}: {Error}.",
                pending.Id, lookup.Error.Code);
            return lookup.Error;
        }

        var member = lookup.Value;

        if (!MatchesRegistration(member, pending))
        {
            // Logged with detail, answered without: the response is deliberately indistinguishable
            // from "no such member" so it can't be used to probe whose SSN this is.
            logger.LogWarning(
                "Facets returned member {MemberId} in tenant {Tenant} but the details did not match "
                + "pending registration {UserId}.",
                member.MemberId, member.Tenant, pending.Id);
            return RegistrationErrors.MemberDetailsMismatch;
        }

        var userId = await userRepository.CreateUserAsync(
            new NewUserRegistration(
                Id: pending.Id,
                Email: pending.Email,
                Username: pending.Email,
                PasswordHash: pending.PasswordHash!,
                PasswordSalt: pending.PasswordSalt!,
                FirstName: pending.FirstName,
                LastName: pending.LastName,
                DateOfBirth: pending.DateOfBirth,
                ZipCode: pending.ZipCode,
                SubscriberId: member.SubscriberId,
                PlanId: member.Plan.PlanId,
                // Only the last four are kept — see User.SsnLast4.
                SsnLast4: ssn[^4..]),
            cancellationToken);

        await pendingRepository.DeleteAsync(pending.Id, cancellationToken);

        logger.LogInformation(
            "Registration completed for user {UserId} (subscriber {SubscriberId}, plan {PlanId}).",
            userId, member.SubscriberId, member.Plan.PlanId);

        return new CompleteRegistrationResult(
            Complete: true,
            UserId: userId,
            MemberInfo: new MemberInfo(
                member.SubscriberId,
                member.MemberId,
                member.FirstName,
                member.LastName,
                pending.Email),
            PlanInfo: new PlanInfo(
                member.Plan.PlanId,
                member.Plan.PlanName,
                member.Plan.LineOfBusiness,
                member.Plan.EffectiveDate));
    }

    /// <summary>Strips formatting so "123-45-6789" and "123456789" are the same value.</summary>
    private static string NormalizeSsn(string ssn) =>
        new(ssn.Where(char.IsDigit).ToArray());

    /// <summary>
    /// Cross-checks what Facets returned against what the member typed at the start. Facets matching on
    /// SSN alone is not enough to hand over an account — surname and date of birth have to agree too.
    /// Names are compared case- and whitespace-insensitively; ZIP is not compared, since members move
    /// and the address on file goes stale.
    /// </summary>
    private static bool MatchesRegistration(FacetsMember member, PendingRegistration pending) =>
        member.DateOfBirth == pending.DateOfBirth
        && string.Equals(
            member.LastName.Trim(),
            pending.LastName.Trim(),
            StringComparison.OrdinalIgnoreCase);
}
