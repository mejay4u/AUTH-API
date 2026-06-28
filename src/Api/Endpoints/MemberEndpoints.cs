using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using AuthApi.Infrastructure.Security.Jwt;

namespace AuthApi.Api.Endpoints;

/// <summary>
/// Protected endpoints used to verify that an issued access token works. <c>GET /me</c> simply
/// projects the claims from the validated JWT — a convenient way to test authentication/authorization
/// end-to-end and an example of how downstream microservices read LOB/Plan claims.
/// </summary>
public static class MemberEndpoints
{
    public sealed record MeResponse(
        string? MemberId,
        string? Username,
        string? Email,
        string? FirstName,
        string? LastName,
        IReadOnlyCollection<string> Lobs,
        IReadOnlyCollection<int> PlanIds);

    public static IEndpointRouteBuilder MapMemberEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/members")
            .WithTags("Members")
            .RequireAuthorization();

        group.MapGet("/me", GetMe)
            .WithName("GetCurrentMember")
            .WithSummary("Return the authenticated member's details, read from the JWT claims.")
            .Produces<MeResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        return app;
    }

    private static IResult GetMe(ClaimsPrincipal user)
    {
        var lobs = user.FindAll(TokenService.LobClaimType).Select(c => c.Value).ToArray();
        var planIds = user.FindAll(TokenService.PlanClaimType)
            .Select(c => int.TryParse(c.Value, out var id) ? id : (int?)null)
            .Where(id => id is not null)
            .Select(id => id!.Value)
            .ToArray();

        var response = new MeResponse(
            MemberId: user.FindFirstValue(JwtRegisteredClaimNames.Sub) ?? user.FindFirstValue(ClaimTypes.NameIdentifier),
            Username: user.FindFirstValue(JwtRegisteredClaimNames.UniqueName) ?? user.Identity?.Name,
            Email: user.FindFirstValue(JwtRegisteredClaimNames.Email),
            FirstName: user.FindFirstValue(JwtRegisteredClaimNames.GivenName),
            LastName: user.FindFirstValue(JwtRegisteredClaimNames.FamilyName),
            Lobs: lobs,
            PlanIds: planIds);

        return Results.Ok(response);
    }
}
