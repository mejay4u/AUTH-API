using System.Globalization;
using System.Security.Cryptography;
using AuthApi.Application.Common.Interfaces;
using AuthApi.Application.Sso;
using AuthApi.Infrastructure.Sso.OpenToken;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AuthApi.Infrastructure.Sso;

/// <summary>
/// The real PingFederate boundary — the port of the legacy <c>PingFedService</c>. Each method builds
/// the connection's user-info attribute set exactly as the legacy method did, generates an OpenToken
/// with the agent configuration named by the SSO row's <c>AgentFileLocationPath</c>, and returns the
/// COMPLETE sign-on URL: the connection's base URL with the token appended as its query parameter
/// (the parameter name comes from the agent file's <c>token-name</c>, e.g. <c>JivaZeomegaOpenToken</c>
/// for the Jiva connections, <c>AbarcaOpentoken</c> for Abarca, <c>opentoken</c> for the rest).
/// The token is appended as a proper query parameter, which removes the legacy
/// <c>Replace("opentoken%0D=", ...)</c> URL-mangling fix-ups.
/// The legacy <c>ParseSSOTokenCSR</c> flow is deliberately not ported.
/// </summary>
public sealed class OpenTokenPingFederateService(
    IOptions<SsoOptions> options,
    IDateTimeProvider clock,
    ILogger<OpenTokenPingFederateService> logger) : IPingFederateService
{
    // Standard OpenToken lifetime attributes. The agent SDK stamps these on every token it writes and
    // the receiving PingFederate adapter validates them — a token without them is rejected.
    private const string NotBeforeAttribute = "not-before";
    private const string NotOnOrAfterAttribute = "not-on-or-after";
    private const string RenewUntilAttribute = "renew-until";
    private const string TimestampFormat = "yyyy-MM-dd'T'HH:mm:ss'Z'";

    /// <summary>Legacy parity: Jiva receives the member id suffixed with the subscriber sequence.</summary>
    private const string JivaMemberIdSuffix = "-01";

    /// <summary>Legacy parity: Softheon receives only the first nine characters of the member id.</summary>
    private const int SoftheonMemberIdLength = 9;

    // Legacy GetCertifiSSO: String.Format("{0}{1}", SSOPingFedURL, member ? ... : ...) — the suffix is
    // concatenated directly onto the configured URL, no separator.
    private const string CertifiMemberUrlSuffix = "goToViewPayInvoice";
    private const string CertifiDependentsUrlSuffix = "viewEntityList";

    public Task<string?> GetAbarcaSsoAsync(SsoUrlContext context, CancellationToken cancellationToken)
    {
        var memberId = context.Member.PrimaryMemberId();

        // Legacy GetAbarcaSSO: subject/memberID plus the LOB's Argus PCN.
        var attributes = new Dictionary<string, string>
        {
            ["subject"] = memberId,
            ["memberID"] = memberId,
            ["PCN"] = context.Configurations[0].ArgusCustomerId ?? string.Empty
        };

        return BuildSsoUrl(context, context.Configurations[0].PingFedUrl, attributes);
    }

    public Task<string?> GetHraJivaSsoAsync(SsoUrlContext context, CancellationToken cancellationToken)
    {
        var member = context.Member;
        var memberId = member.PrimaryMemberId();

        // Legacy GetHRAJivaSSO. "Source" carries the resolved assessment name — the handler has
        // already applied the age-based HRA assessment rule to the first configuration row.
        var attributes = new Dictionary<string, string>
        {
            ["subject"] = memberId,
            ["UserID"] = memberId,
            ["FirstName"] = member.FirstName ?? string.Empty,
            ["LastName"] = member.LastName ?? string.Empty,
            ["UserRoles"] = "MEMBER",
            ["lob"] = context.Lob,
            ["MemberID"] = memberId + JivaMemberIdSuffix,
            ["Source"] = context.Configurations[0].AssessmentName ?? string.Empty
        };

        return BuildSsoUrl(context, context.Configurations[0].PingFedUrl, attributes);
    }

    public Task<string?> GetPlanOfCareSsoAsync(SsoUrlContext context, CancellationToken cancellationToken)
    {
        var member = context.Member;

        // Legacy GetPlanOfCareSSO: a designee (role "D") signs on with their own id and role; a
        // member with theirs. Both carry the LOB.
        var attributes = member.IsMember || string.IsNullOrEmpty(member.DesigneeId)
            ? new Dictionary<string, string>
            {
                ["subject"] = member.PrimaryMemberId(),
                ["UserNID"] = member.PrimaryMemberId(),
                ["UserRole"] = "memberportal-member",
                ["LOB"] = context.Lob
            }
            : new Dictionary<string, string>
            {
                ["subject"] = member.DesigneeId,
                ["UserNID"] = member.DesigneeId,
                ["UserRole"] = "memberportal-designee",
                ["LOB"] = context.Lob
            };

        return BuildSsoUrl(context, context.Configurations[0].PingFedUrl, attributes);
    }

    public Task<string?> GetChatSsoAsync(SsoUrlContext context, CancellationToken cancellationToken)
    {
        var chat = options.Value.Chat;
        var member = context.Member;
        var memberId = member.PrimaryMemberId();

        // Legacy GetChatSSO: MemberIdWithoutSuffix is the id before the trailing "-NN" subscriber
        // suffix; memberSFX is the suffix's last digit ("0" when there is none or off-exchange).
        var dash = memberId.LastIndexOf('-');
        var memberIdWithoutSuffix = dash > 0 ? memberId[..dash] : memberId;
        var suffix = dash > 0 ? memberId[(dash + 1)..] : string.Empty;
        var lastDigit = suffix.Length > 0 ? suffix[^1].ToString() : "0";

        var attributes = new Dictionary<string, string> { ["subject"] = memberId };
        if (member.IsExchange)
        {
            attributes["memberId"] = EncryptChatValue(memberIdWithoutSuffix, chat);
            attributes["memberSFX"] = EncryptChatValue(lastDigit, chat);
        }
        else
        {
            attributes["memberId"] = EncryptChatValue(memberId, chat);
            attributes["memberSFX"] = EncryptChatValue("0", chat);
        }

        attributes["LOB"] = EncryptChatValue(context.Lob, chat);

        // Legacy parity: nonce is 24 characters of base64 randomness concatenated straight onto the
        // configured URL (the DB value ends in '?' or '&'), followed by the configured state.
        var nonce = Convert.ToBase64String(RandomNumberGenerator.GetBytes(24))[..24];
        var baseUrl = string.Concat(
            context.Configurations[0].PingFedUrl, "nonce=", nonce, "&state=", chat.State);

        return BuildSsoUrl(context, baseUrl, attributes);
    }

    public Task<string?> GetSoftheonSsoAsync(SsoUrlContext context, CancellationToken cancellationToken)
    {
        var memberId = context.Member.PrimaryMemberId();
        var trimmedMemberId = memberId.Length > SoftheonMemberIdLength
            ? memberId[..SoftheonMemberIdLength]
            : memberId;

        // Legacy GetSoftheonSSO.
        var attributes = new Dictionary<string, string>
        {
            ["subject"] = trimmedMemberId,
            ["memberID"] = trimmedMemberId,
            ["idType"] = "IssuerId",
            ["userRole"] = "Individual",
            ["sender"] = "AmeriHealthCaritas"
        };

        return BuildSsoUrl(context, context.Configurations[0].PingFedUrl, attributes);
    }

    public Task<string?> GetCertifiSsoAsync(
        SsoUrlContext context,
        SsoAudience audience,
        IReadOnlyDictionary<string, string> attributes,
        CancellationToken cancellationToken)
    {
        var suffix = audience == SsoAudience.Member ? CertifiMemberUrlSuffix : CertifiDependentsUrlSuffix;
        return BuildSsoUrl(context, context.Configurations[0].PingFedUrl + suffix, attributes);
    }

    public Task<string?> GetSdsSsoAsync(
        SsoUrlContext context,
        SsoAudience audience,
        IReadOnlyDictionary<string, string> attributes,
        CancellationToken cancellationToken) =>
        // Legacy GetSDSSSO appended an empty suffix for both audiences.
        BuildSsoUrl(context, context.Configurations[0].PingFedUrl, attributes);

    public Task<string?> GenerateSsoTokenAsync(
        string agentFileName,
        IReadOnlyDictionary<string, string> attributes,
        CancellationToken cancellationToken)
    {
        try
        {
            var agent = PingFederateAgentConfig.Load(ResolveAgentFilePath(agentFileName));
            return Task.FromResult<string?>(WriteToken(agent, attributes));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to generate an OpenToken with agent file {AgentFile}.", agentFileName);
            return Task.FromResult<string?>(null);
        }
    }

    public Task<IReadOnlyDictionary<string, IReadOnlyList<string>>?> ParseSsoTokenAsync(
        string agentFileName,
        string token,
        CancellationToken cancellationToken)
    {
        try
        {
            var agent = PingFederateAgentConfig.Load(ResolveAgentFilePath(agentFileName));
            var attributes = OpenTokenReader.Read(token, agent.SharedSecret);

            if (!IsWithinLifetime(attributes, agent))
            {
                logger.LogWarning("Inbound OpenToken for agent file {AgentFile} is outside its validity window.", agentFileName);
                return Task.FromResult<IReadOnlyDictionary<string, IReadOnlyList<string>>?>(null);
            }

            return Task.FromResult<IReadOnlyDictionary<string, IReadOnlyList<string>>?>(attributes);
        }
        catch (Exception ex)
        {
            // The token value itself is never logged: it carries member PII.
            logger.LogError(ex, "Failed to parse an inbound OpenToken with agent file {AgentFile}.", agentFileName);
            return Task.FromResult<IReadOnlyDictionary<string, IReadOnlyList<string>>?>(null);
        }
    }

    private Task<string?> BuildSsoUrl(
        SsoUrlContext context, string? baseUrl, IEnumerable<KeyValuePair<string, string>> userInfo)
    {
        var configuration = context.Configurations[0];

        try
        {
            if (string.IsNullOrWhiteSpace(baseUrl))
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
            var token = WriteToken(agent, userInfo);

            var separator = baseUrl.Contains('?') ? '&' : '?';
            return Task.FromResult<string?>($"{baseUrl}{separator}{agent.TokenName}={token}");
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

    /// <summary>
    /// Writes the token with the standard lifetime attributes appended, driven by the agent file's
    /// <c>token-lifetime</c>/<c>renew-until</c> — agent SDK parity. Caller-supplied values win.
    /// </summary>
    private string WriteToken(PingFederateAgentConfig agent, IEnumerable<KeyValuePair<string, string>> userInfo)
    {
        var attributes = new List<KeyValuePair<string, string>>(userInfo);
        var supplied = attributes.Select(a => a.Key).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var now = clock.UtcNow;

        AddIfMissing(NotBeforeAttribute, now);
        AddIfMissing(NotOnOrAfterAttribute, now + agent.TokenLifetime);
        AddIfMissing(RenewUntilAttribute, now + agent.RenewUntil);

        return OpenTokenWriter.Write(attributes, agent.SharedSecret, agent.CipherSuite);

        void AddIfMissing(string name, DateTime value)
        {
            if (!supplied.Contains(name))
            {
                attributes.Add(new(name, value.ToString(TimestampFormat, CultureInfo.InvariantCulture)));
            }
        }
    }

    /// <summary>
    /// Validates an inbound token's lifetime attributes the way the agent SDK does: not yet valid
    /// (with the agent's clock-skew tolerance) or expired tokens are rejected. Tokens without
    /// lifetime attributes are accepted for legacy compatibility.
    /// </summary>
    private bool IsWithinLifetime(IReadOnlyDictionary<string, IReadOnlyList<string>> attributes, PingFederateAgentConfig agent)
    {
        var now = clock.UtcNow;

        if (TryGetTimestamp(attributes, NotBeforeAttribute, out var notBefore) &&
            now < notBefore - agent.NotBeforeTolerance)
        {
            return false;
        }

        if (TryGetTimestamp(attributes, NotOnOrAfterAttribute, out var notOnOrAfter) &&
            now >= notOnOrAfter)
        {
            return false;
        }

        return true;
    }

    private static bool TryGetTimestamp(
        IReadOnlyDictionary<string, IReadOnlyList<string>> attributes, string name, out DateTime value)
    {
        value = default;
        return attributes.TryGetValue(name, out var values)
            && values.Count > 0
            && DateTime.TryParse(
                values[0],
                CultureInfo.InvariantCulture,
                DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal,
                out value);
    }

    /// <summary>
    /// The legacy <c>Utility.encryptStringToBytes_AES_Salesforce</c>: AES-CBC with a configured
    /// key/IV, base64-encoded. The key material must match what the chat provider decrypts with.
    /// </summary>
    private static string EncryptChatValue(string value, ChatSsoOptions chat)
    {
        if (string.IsNullOrEmpty(chat.AesKeyBase64) || string.IsNullOrEmpty(chat.AesIvBase64))
        {
            throw new InvalidOperationException(
                "Sso:Chat:AesKeyBase64 and Sso:Chat:AesIvBase64 must be configured for CHATSSO.");
        }

        using var aes = Aes.Create();
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;
        aes.Key = Convert.FromBase64String(chat.AesKeyBase64);
        aes.IV = Convert.FromBase64String(chat.AesIvBase64);

        using var encryptor = aes.CreateEncryptor();
        var plain = System.Text.Encoding.UTF8.GetBytes(value);
        return Convert.ToBase64String(encryptor.TransformFinalBlock(plain, 0, plain.Length));
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
