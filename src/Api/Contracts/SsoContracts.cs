using AuthApi.Application.Sso;

namespace AuthApi.Api.Contracts;

/// <summary>
/// Response for <c>GET /api/v1/sso</c> — the reshaped legacy <c>SSOResponseModel</c>.
/// <c>SsoUrl</c> carries the generated sign-on URL directly (the legacy API smuggled it into
/// <c>Data[0].SSOPingFedReturnURL</c>, which is also still populated for compatibility).
/// Internal fields (<c>SSOConfigID</c>, agent file paths) are deliberately not exposed.
/// </summary>
public sealed record SsoResponse(
    string MemberId,
    string? DesigneeId,
    string Role,
    string Lob,
    string SsoName,
    string? AssessmentName,
    string? SsoUrl,
    IReadOnlyList<SsoConfigItem> Data)
{
    public static SsoResponse From(SsoResult result) => new(
        result.Member.PrimaryMemberId(),
        result.Member.DesigneeId,
        result.Member.Role,
        result.Lob,
        result.SsoName,
        result.AssessmentName,
        result.SsoUrl,
        result.Configurations.Select(SsoConfigItem.From).ToArray());
}

/// <summary>One SSO configuration row (the reshaped legacy <c>SSOData</c>).</summary>
public sealed record SsoConfigItem(
    string SsoName,
    string? Description,
    string? PingFedUrl,
    string? PingFedReturnUrl,
    string? AssessmentName,
    string? Level,
    string? ArgusCustomerId)
{
    public static SsoConfigItem From(SsoConfigurationEntry entry) => new(
        entry.SsoName,
        entry.Description,
        entry.PingFedUrl,
        entry.PingFedReturnUrl,
        entry.AssessmentName,
        entry.Level,
        entry.ArgusCustomerId);
}
