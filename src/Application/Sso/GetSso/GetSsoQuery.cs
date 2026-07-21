using AuthApi.Domain.Common;
using MediatR;

namespace AuthApi.Application.Sso.GetSso;

/// <summary>
/// Resolves the SSO configuration for a LOB + SSO name and, unless the LOB is in the skip list,
/// generates the federated sign-on URL. The clean-architecture replacement for the legacy
/// <c>MemberController.GetSSO</c> → <c>MemberService.GetSSOPerLOB</c> pipeline.
/// <paramref name="Member"/> is built from the caller's JWT claims, never from the request body.
/// </summary>
public sealed record GetSsoQuery(
    string Lob,
    string? PlanCode,
    string SsoName,
    SsoMemberContext Member) : IRequest<Result<SsoResult>>;
