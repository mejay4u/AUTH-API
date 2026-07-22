using AuthApi.Application.Sso;
using AuthApi.Infrastructure.Sso.OpenToken;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AuthApi.Infrastructure.Sso;

/// <summary>
/// The real PingFederate boundary — the port of the legacy <c>PingFedService</c>. Each method builds
/// the connection's user-info attribute set, generates an OpenToken with the agent configuration named
/// by the SSO row's <c>AgentFileLocationPath</c>, and returns the COMPLETE sign-on URL: the configured
/// <c>PingFedUrl</c> with the token appended as its query parameter, e.g.
/// <c>https://fs-uat.../idp/startSSO.ping?PartnerSpId=...&amp;JivaZeomegaOpenToken=&lt;token&gt;</c>.
/// The token is appended as a proper query parameter, which removes the legacy
/// <c>Replace("JivaZeomegaOpenToken%3D=", ...)</c> URL-mangling fix-up.
/// </summary>
public sealed class OpenTokenPingFederateService(
    IOptions<SsoOptions> options,
    ILogger<OpenTokenPingFederateService> logger) : IPingFederateService
{
    /// <summary>Legacy parity: Jiva receives the member id suffixed with the subscriber sequence.</summary>
    private const string JivaMemberIdSuffix = "-01";

    public Task<string?> GetAbarcaSsoAsync(SsoUrlContext context, CancellationToken cancellationToken) =>
        BuildSsoUrlAsync(context, BaseMemberAttributes(context));

    public Task<string?> GetHraJivaSsoAsync(SsoUrlContext context, CancellationToken cancellationToken) =>
        BuildSsoUrlAsync(context, JivaAttributes(context));

    public Task<string?> GetPlanOfCareSsoAsync(SsoUrlContext context, CancellationToken cancellationToken) =>
        BuildSsoUrlAsync(context, JivaAttributes(context));

    public Task<string?> GetChatSsoAsync(SsoUrlContext context, CancellationToken cancellationToken) =>
        BuildSsoUrlAsync(context, BaseMemberAttributes(context));

    public Task<string?> GetSoftheonSsoAsync(SsoUrlContext context, CancellationToken cancellationToken) =>
        BuildSsoUrlAsync(context, BaseMemberAttributes(context));

    public Task<string?> GetCertifiSsoAsync(
        SsoUrlContext context,
        SsoAudience audience,
        IReadOnlyDictionary<string, string> attributes,
        CancellationToken cancellationToken) =>
        BuildSsoUrlAsync(context, attributes);

    public Task<string?> GetSdsSsoAsync(
        SsoUrlContext context,
        SsoAudience audience,
        IReadOnlyDictionary<string, string> attributes,
        CancellationToken cancellationToken) =>
        BuildSsoUrlAsync(context, attributes);

    /// <summary>
    /// The user-info set the legacy <c>GetHRAJivaSSO</c>/<c>GetPlanOfCareSSO</c> wrote into the token
    /// for the Jiva (ZeOmega) connections. <c>Source</c> carries the resolved assessment name — the
    /// handler has already applied the age-based HRA assessment rule to the first configuration row.
    /// </summary>
    private static Dictionary<string, string> JivaAttributes(SsoUrlContext context)
    {
        var attributes = BaseMemberAttributes(context);
        attributes["lob"] = context.Lob;
        attributes["MemberID"] = context.Member.PrimaryMemberId() + JivaMemberIdSuffix;
        attributes["Source"] = context.Configurations[0].AssessmentName ?? string.Empty;
        return attributes;
    }

    private static Dictionary<string, string> BaseMemberAttributes(SsoUrlContext context)
    {
        var member = context.Member;
        var memberId = member.PrimaryMemberId();

        // Insertion order is the payload order; "subject" (the OpenToken subject) must be present.
        return new Dictionary<string, string>
        {
            ["subject"] = memberId,
            ["UserID"] = memberId,
            ["FirstName"] = member.FirstName ?? string.Empty,
            ["LastName"] = member.LastName ?? string.Empty,
            ["UserRoles"] = "MEMBER"
        };
    }

    private Task<string?> BuildSsoUrlAsync(SsoUrlContext context, IEnumerable<KeyValuePair<string, string>> userInfo)
    {
        var configuration = context.Configurations[0];

        try
        {
            if (string.IsNullOrWhiteSpace(configuration.PingFedUrl))
            {
                logger.LogWarning("SSO {SsoName} for LOB {Lob} has no PingFedUrl configured.", context.SsoName, context.Lob);
                return Task.FromResult<string?>(null);
            }

            if (string.IsNullOrWhiteSpace(configuration.AgentFileLocationPath))
            {
                logger.LogWarning(
                    "SSO {SsoName} for LOB {Lob} has no AgentFileLocationPath configured; cannot generate an OpenToken.",
                    context.SsoName, context.Lob);
                return Task.FromResult<string?>(null);
            }

            var agent = PingFederateAgentConfig.Load(ResolveAgentFilePath(configuration.AgentFileLocationPath));
            var token = OpenTokenWriter.Write(userInfo, agent.SharedSecret, agent.CipherSuite);

            var separator = configuration.PingFedUrl.Contains('?') ? '&' : '?';
            return Task.FromResult<string?>($"{configuration.PingFedUrl}{separator}{agent.TokenName}={token}");
        }
        catch (Exception ex)
        {
            // Legacy parity: a failed hand-off never fails the request — the configuration is still
            // returned, just without a generated URL. Attribute values are PII and are not logged.
            logger.LogError(ex,
                "Failed to generate the PingFederate sign-on URL for SSO {SsoName}, LOB {Lob}, agent file {AgentFile}.",
                context.SsoName, context.Lob, configuration.AgentFileLocationPath);
            return Task.FromResult<string?>(null);
        }
    }

    private string ResolveAgentFilePath(string agentFileLocationPath)
    {
        // The database value is a bare file name (e.g. "hra-agent-config-qa.txt"); strip any directory
        // part so a compromised row cannot traverse outside the agent files directory.
        var fileName = Path.GetFileName(agentFileLocationPath);
        var configuredRoot = options.Value.PingFederate.AgentFilesPath;

        string[] candidates = Path.IsPathRooted(configuredRoot)
            ? [Path.Combine(configuredRoot, fileName)]
            :
            [
                // Legacy used Directory.GetCurrentDirectory() (the content root under IIS); the base
                // directory fallback covers published layouts where the two differ.
                Path.Combine(Directory.GetCurrentDirectory(), configuredRoot, fileName),
                Path.Combine(AppContext.BaseDirectory, configuredRoot, fileName)
            ];

        return candidates.FirstOrDefault(File.Exists)
            ?? throw new FileNotFoundException(
                $"PingFederate agent configuration '{fileName}' was not found under '{configuredRoot}'.", fileName);
    }
}
