using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Registration.Application.Common.Interfaces;
using Registration.Application.Common.Models;
using Registration.Domain.Common;
using Registration.Domain.Registration;

namespace Registration.Infrastructure.Facets;

/// <summary>
/// Talks to the Facets-facing service over HTTP to find a member across all tenants.
/// </summary>
/// <remarks>
/// ⚠️ The request and response shapes below (<see cref="MemberSearchRequest"/> /
/// <see cref="MemberSearchResponse"/>) are PROVISIONAL — they were written to the sequence diagram, not
/// to a published Facets contract. Align them with the real service before trusting this; everything
/// else here (auth header, timeout, status-to-error mapping, the not-found vs unreachable distinction)
/// holds regardless of the payload shape.
///
/// Fanning out across tenants is assumed to be the far side's job — one search call, all tenants. If it
/// turns out to be per-tenant, loop here rather than pushing tenant awareness into the use case.
/// </remarks>
public sealed class HttpFacetsClient(
    HttpClient httpClient,
    IOptions<FacetsOptions> options,
    ILogger<HttpFacetsClient> logger) : IFacetsClient
{
    private readonly FacetsOptions _options = options.Value;

    public async Task<Result<FacetsMember>> FindMemberAsync(
        FacetsMemberLookup lookup,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await httpClient.PostAsJsonAsync(
                _options.MemberSearchPath,
                new MemberSearchRequest(
                    lookup.Ssn,
                    lookup.MemberId,
                    lookup.LastName,
                    lookup.DateOfBirth,
                    lookup.ZipCode),
                cancellationToken);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return RegistrationErrors.MemberNotFound;
            }

            if (!response.IsSuccessStatusCode)
            {
                // Deliberately not logging the body: it may echo the SSN back.
                logger.LogError(
                    "Facets member search failed with status {StatusCode}.", (int)response.StatusCode);
                return RegistrationErrors.EligibilityLookupFailed;
            }

            var payload = await response.Content.ReadFromJsonAsync<MemberSearchResponse>(cancellationToken);

            if (payload is null || !payload.Found || payload.Member is null)
            {
                return RegistrationErrors.MemberNotFound;
            }

            return payload.Member.ToDomain();
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogError("Facets member search timed out after {Timeout}s.", _options.TimeoutSeconds);
            return RegistrationErrors.EligibilityLookupFailed;
        }
        catch (HttpRequestException e)
        {
            logger.LogError(e, "Facets member search could not reach the service.");
            return RegistrationErrors.EligibilityLookupFailed;
        }
    }

    // --- Provisional wire contract (see the remarks above) ---

    private sealed record MemberSearchRequest(
        string Ssn,
        string? MemberId,
        string LastName,
        DateOnly DateOfBirth,
        string ZipCode);

    private sealed record MemberSearchResponse(bool Found, MemberSearchMatch? Member);

    private sealed record MemberSearchMatch(
        string SubscriberId,
        string MemberId,
        string FirstName,
        string LastName,
        DateOnly DateOfBirth,
        string ZipCode,
        string Tenant,
        string PlanId,
        string PlanName,
        string? LineOfBusiness,
        DateOnly? EffectiveDate)
    {
        public FacetsMember ToDomain() => new(
            SubscriberId,
            MemberId,
            FirstName,
            LastName,
            DateOfBirth,
            ZipCode,
            Tenant,
            new FacetsPlan(PlanId, PlanName, LineOfBusiness, EffectiveDate));
    }
}
