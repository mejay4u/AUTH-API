namespace AuthApi.Application.Sso;

/// <summary>
/// Well-known SSO names as stored in the SSO configuration data (legacy <c>MEM_SP_SSO_Config</c>).
/// </summary>
public static class SsoNames
{
    public const string Abarca = "ABARCASSO";
    public const string Hra = "HRA";
    public const string PlanOfCare = "PLANOFCARESSO";
    public const string Chat = "CHATSSO";
    public const string Softheon = "SOFTHEONSSO";
    public const string Certifi = "CERTIFISSO";
    public const string Sds = "SDS";
}

/// <summary>
/// Who the federated session is issued for. Replaces the legacy <c>CertifiSSOType</c> and
/// <c>SDSSSOType</c> enums, which were identical.
/// </summary>
public enum SsoAudience
{
    Dependents = 0,
    Member = 1
}

/// <summary>
/// The authenticated caller as needed by the SSO integrations. Built from validated JWT claims at the
/// endpoint — providers never read <c>HttpContext</c> directly (the legacy service did, which made it
/// untestable without a web host).
/// </summary>
public sealed record SsoMemberContext(
    string MemberId,
    string? DesigneeId,
    string Role,
    string? Email,
    string? FirstName,
    string? LastName,
    DateOnly? DateOfBirth,
    IReadOnlyCollection<string> DependentMemberIds,
    bool IsExchange = false)
{
    public const string MemberRole = "Member";

    public bool IsMember => string.Equals(Role, MemberRole, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Member ids arrive as <c>id[:qualifier]</c>; the external identifier is the part before the colon.
    /// </summary>
    public string PrimaryMemberId() => ExtractIdentifier(MemberId);

    public static string ExtractIdentifier(string raw) =>
        raw.Split(':', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault()?.Trim() ?? string.Empty;
}

/// <summary>One row of SSO configuration (legacy <c>SSODetails</c>).</summary>
public sealed record SsoConfigurationEntry(
    int SsoConfigId,
    string SsoName,
    string? Description,
    string? PingFedUrl,
    string? PingFedReturnUrl,
    string? AgentFileLocationPath,
    string? AssessmentName,
    string? Level,
    bool Active,
    DateTime? EffectiveDate,
    DateTime? TermDate,
    string? ArgusCustomerId);

/// <summary>Everything an <see cref="ISsoUrlProvider"/> needs to produce a sign-on URL.</summary>
public sealed record SsoUrlContext(
    string Lob,
    string? PlanCode,
    string SsoName,
    SsoMemberContext Member,
    IReadOnlyList<SsoConfigurationEntry> Configurations);

/// <summary>Outcome of the GetSso use case.</summary>
public sealed record SsoResult(
    string Lob,
    string SsoName,
    string? AssessmentName,
    string? SsoUrl,
    SsoMemberContext Member,
    IReadOnlyList<SsoConfigurationEntry> Configurations);
