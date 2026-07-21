namespace AuthApi.Application.Sso.Providers;

/// <summary>
/// SDS hand-off. Same member/dependents split as CERTIFI but with the attribute names SDS expects:
/// <c>subject</c>, <c>Email</c>, and <c>EnrolleeId</c> (the member's external id, or a comma-separated
/// list of dependent ids for a designee).
/// </summary>
public sealed class SdsSsoUrlProvider(IPingFederateService pingFed) : ISsoUrlProvider
{
    public string SsoName => SsoNames.Sds;

    public Task<string?> GetSsoUrlAsync(SsoUrlContext context, CancellationToken cancellationToken)
    {
        var member = context.Member;

        var attributes = new Dictionary<string, string>
        {
            ["subject"] = member.Email ?? string.Empty,
            ["Email"] = member.Email ?? string.Empty
        };

        SsoAudience audience;
        if (member.IsMember)
        {
            audience = SsoAudience.Member;
            attributes["EnrolleeId"] = member.PrimaryMemberId();
        }
        else
        {
            audience = SsoAudience.Dependents;
            attributes["EnrolleeId"] = string.Join(",", member.DependentMemberIds);
        }

        return pingFed.GetSdsSsoAsync(context, audience, attributes, cancellationToken);
    }
}
