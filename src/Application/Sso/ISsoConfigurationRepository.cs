namespace AuthApi.Application.Sso;

/// <summary>
/// Data-access boundary for SSO configuration (legacy <c>MemberRepository.FetchSSOData</c>).
/// Implementations return the FULL configuration set for a LOB — filtering by SSO name happens in
/// memory, so one cached load serves every SSO integration on the portal.
/// </summary>
public interface ISsoConfigurationRepository
{
    Task<IReadOnlyList<SsoConfigurationEntry>> GetForLobAsync(
        string lob, string? planCode, CancellationToken cancellationToken);
}
