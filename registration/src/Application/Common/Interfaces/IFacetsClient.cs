using Registration.Application.Common.Models;
using Registration.Domain.Common;

namespace Registration.Application.Common.Interfaces;

/// <summary>
/// Looks a member up in Facets, across all tenants, to confirm eligibility at the final registration
/// step. Kept as an abstraction so the use case never learns how Facets is reached (HTTP, a broker,
/// a per-tenant fan-out) and so it can be faked in tests.
/// </summary>
public interface IFacetsClient
{
    /// <summary>
    /// Find the member matching the supplied identifiers. Returns
    /// <see cref="Domain.Registration.RegistrationErrors.MemberNotFound"/> when nothing matches and
    /// <see cref="Domain.Registration.RegistrationErrors.EligibilityLookupFailed"/> when Facets could
    /// not be reached — the caller treats those very differently, so don't collapse them.
    /// </summary>
    Task<Result<FacetsMember>> FindMemberAsync(FacetsMemberLookup lookup, CancellationToken cancellationToken);
}
