using AuthApi.Application.Sso;

namespace AuthApi.Infrastructure.Sso;

/// <summary>
/// In-memory SSO configuration used when <c>Database:Provider=InMemory</c>, so the endpoint can be
/// exercised end-to-end (Swagger, requests.http) without the real member database or PingFederate.
/// </summary>
public sealed class MockSsoConfigurationRepository : ISsoConfigurationRepository
{
    private static readonly string[] KnownSsoNames =
    [
        SsoNames.Abarca,
        SsoNames.Hra,
        SsoNames.PlanOfCare,
        SsoNames.Chat,
        SsoNames.Softheon,
        SsoNames.Certifi,
        SsoNames.Sds
    ];

    public Task<IReadOnlyList<SsoConfigurationEntry>> GetForLobAsync(
        string lob, string? planCode, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;

        IReadOnlyList<SsoConfigurationEntry> entries = KnownSsoNames
            .Select((name, index) => new SsoConfigurationEntry(
                SsoConfigId: index + 1,
                SsoName: name,
                Description: $"{name} (mock configuration for LOB {lob})",
                PingFedUrl: $"https://pingfed.example.local/idp/startSSO.ping?sso={name.ToLowerInvariant()}&lob={lob}",
                PingFedReturnUrl: $"https://portal.example.local/sso/{name.ToLowerInvariant()}/return",
                AgentFileLocationPath: "mock-agent-config-qa.txt",
                AssessmentName: name == SsoNames.Hra ? "GeneralAssessment" : null,
                Level: "1",
                Active: true,
                EffectiveDate: now.AddYears(-1),
                TermDate: now.AddYears(1),
                ArgusCustomerId: name == SsoNames.Abarca ? "MOCK-ARGUS-01" : null))
            .ToArray();

        return Task.FromResult(entries);
    }
}
