namespace AuthApi.Application.Sso.Providers;

/// <summary>
/// CERTIFI hand-off. A member gets a personal session (subject/email/name attributes plus their
/// external identifier); any other role (designee) gets a session scoped to the member's dependents.
/// Dependent ids come from the caller's JWT — already parsed into <see cref="SsoMemberContext"/> —
/// where the legacy service read the raw <c>dependentmemberid</c> claim off <c>HttpContext</c> here.
/// </summary>
public sealed class CertifiSsoUrlProvider(IPingFederateService pingFed) : ISsoUrlProvider
{
    public string SsoName => SsoNames.Certifi;

    public Task<string?> GetSsoUrlAsync(SsoUrlContext context, CancellationToken cancellationToken)
    {
        var member = context.Member;

        var attributes = new Dictionary<string, string>
        {
            ["subject"] = member.Email ?? string.Empty,
            ["userEmail"] = member.Email ?? string.Empty,
            ["userLastName"] = member.LastName ?? string.Empty,
            ["userFirstName"] = member.FirstName ?? string.Empty
        };

        SsoAudience audience;
        if (member.IsMember)
        {
            audience = SsoAudience.Member;
            attributes["entityType"] = "person";
            attributes["externalIdentifier"] = member.PrimaryMemberId();
        }
        else
        {
            audience = SsoAudience.Dependents;
            attributes["persons"] = string.Join(",", member.DependentMemberIds);
        }

        return pingFed.GetCertifiSsoAsync(context, audience, attributes, cancellationToken);
    }
}
