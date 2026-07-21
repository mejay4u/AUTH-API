using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using AuthApi.Api.Contracts;
using AuthApi.Api.Extensions;
using AuthApi.Application.Sso;
using AuthApi.Application.Sso.GetSso;
using MediatR;

namespace AuthApi.Api.Endpoints;

/// <summary>
/// Federated SSO endpoints — the rewrite of the legacy <c>MemberController.GetSSO</c>, kept as its own
/// endpoint group (own route prefix, own tag) instead of being mixed into the auth or member endpoints.
/// Member identity (id, email, name, role, dependents) is read from the validated JWT, never from the
/// request, so a caller cannot request an SSO hand-off for someone else.
/// </summary>
public static class SsoEndpoints
{
    /// <summary>Claim carrying dependent member ids as comma-separated <c>id[:qualifier]</c> entries.</summary>
    private const string DependentMemberIdClaimType = "dependentmemberid";

    public static IEndpointRouteBuilder MapSsoEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/sso")
            .WithTags("SSO")
            .RequireAuthorization();

        group.MapGet("/", GetSsoAsync)
            .WithName("GetSso")
            .WithSummary("Resolve the SSO configuration and federated sign-on URL for a LOB + SSO name.")
            .Produces<SsoResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return app;
    }

    private static async Task<IResult> GetSsoAsync(
        string lob,
        string ssoName,
        string? planCode,
        string? designeeId,
        DateOnly? dateOfBirth,
        ClaimsPrincipal user,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var member = BuildMemberContext(user, designeeId, dateOfBirth);
        var result = await sender.Send(new GetSsoQuery(lob, planCode, ssoName, member), cancellationToken);
        return result.ToHttpResult(SsoResponse.From);
    }

    private static SsoMemberContext BuildMemberContext(
        ClaimsPrincipal user, string? designeeId, DateOnly? dateOfBirthFallback)
    {
        var memberId = user.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? user.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? string.Empty;

        var role = user.FindFirstValue(ClaimTypes.Role)
            ?? user.FindFirstValue("role")
            ?? SsoMemberContext.MemberRole;

        // Prefer a birthdate claim; the query-string value (legacy parity — the old API took DOB on the
        // request) is only a fallback and only influences which HRA assessment name is returned.
        DateOnly? dateOfBirth =
            DateOnly.TryParse(user.FindFirstValue(JwtRegisteredClaimNames.Birthdate), out var fromClaim)
                ? fromClaim
                : dateOfBirthFallback;

        var dependentIds = (user.FindFirstValue(DependentMemberIdClaimType) ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(SsoMemberContext.ExtractIdentifier)
            .Where(id => id.Length > 0)
            .ToArray();

        return new SsoMemberContext(
            memberId,
            designeeId,
            role,
            user.FindFirstValue(JwtRegisteredClaimNames.Email),
            user.FindFirstValue(JwtRegisteredClaimNames.GivenName),
            user.FindFirstValue(JwtRegisteredClaimNames.FamilyName),
            dateOfBirth,
            dependentIds);
    }
}
