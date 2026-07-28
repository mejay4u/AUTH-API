using Microsoft.Extensions.Logging;
using Registration.Application.Common.Interfaces;
using Registration.Application.Common.Models;
using Registration.Domain.Common;
using Registration.Domain.Registration;

namespace Registration.Infrastructure.Facets;

/// <summary>
/// A synthetic <see cref="IFacetsClient"/> for walking the flow end-to-end before the real Facets
/// integration exists (Facets:Provider = "Stub"). It echoes the details it was given back as a match,
/// deriving stable-looking subscriber and plan ids from the SSN.
/// </summary>
/// <remarks>
/// It matches everyone, so it must never be enabled outside development — the whole point of phase 4
/// is that not everyone is eligible. One exception exists so the unhappy path is testable: an SSN
/// ending in <c>0000</c> is treated as "no such member".
/// </remarks>
public sealed class StubFacetsClient(ILogger<StubFacetsClient> logger) : IFacetsClient
{
    public Task<Result<FacetsMember>> FindMemberAsync(
        FacetsMemberLookup lookup,
        CancellationToken cancellationToken)
    {
        logger.LogWarning(
            "Facets STUB in use — every lookup matches. Do not enable this outside development.");

        var digits = new string(lookup.Ssn.Where(char.IsDigit).ToArray());

        if (digits.EndsWith("0000", StringComparison.Ordinal))
        {
            return Task.FromResult(Result.Failure<FacetsMember>(RegistrationErrors.MemberNotFound));
        }

        var suffix = digits.Length >= 4 ? digits[^4..] : digits.PadLeft(4, '0');

        var member = new FacetsMember(
            SubscriberId: $"SUB{suffix}",
            MemberId: lookup.MemberId ?? $"MBR{suffix}",
            FirstName: lookup.FirstName,
            LastName: lookup.LastName,
            DateOfBirth: lookup.DateOfBirth,
            ZipCode: lookup.ZipCode,
            Tenant: "stub",
            Plan: new FacetsPlan(
                PlanId: $"PLN{suffix}",
                PlanName: "Stub Health Plan",
                LineOfBusiness: "COMMERCIAL",
                EffectiveDate: DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(-6))));

        return Task.FromResult(Result.Success(member));
    }
}
