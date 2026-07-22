namespace AuthApi.Application.Sso;

/// <summary>
/// SSO feature configuration, bound from the "Sso" section. Collects the settings the legacy code
/// read ad hoc from <c>IConfiguration</c> (<c>SkipSSO:Lobs</c>, <c>SSOAssessmentSettings:*</c>) into
/// one validated options class.
/// </summary>
public sealed class SsoOptions
{
    public const string SectionName = "Sso";

    /// <summary>
    /// LOBs for which the PingFederate hand-off is skipped (legacy <c>SkipSSO:Lobs</c>).
    /// Configuration rows are still returned, but no sign-on URL is generated.
    /// </summary>
    public string[] SkipLobs { get; init; } = [];

    /// <summary>Sliding lifetime of the cached per-LOB configuration load (legacy: 1 day).</summary>
    public TimeSpan ConfigCacheSlidingExpiration { get; init; } = TimeSpan.FromDays(1);

    public HraAssessmentOptions HraAssessment { get; init; } = new();

    public PingFederateOptions PingFederate { get; init; } = new();
}

/// <summary>
/// PingFederate OpenToken hand-off settings. The agent configuration files referenced by each SSO
/// row's <c>AgentFileLocationPath</c> live under <see cref="AgentFilesPath"/> (legacy
/// <c>App_Data/AgentFiles</c>).
/// </summary>
public sealed class PingFederateOptions
{
    /// <summary>
    /// When false the stub implementation is used: the configured URL is returned without an
    /// OpenToken, so no complete sign-on URL is generated.
    /// </summary>
    public bool Enabled { get; init; } = true;

    /// <summary>
    /// Directory holding the agent-config .txt files downloaded from the PingFederate admin console.
    /// A relative path is resolved against the content root (and the app base directory as fallback).
    /// </summary>
    public string AgentFilesPath { get; init; } = Path.Combine("App_Data", "AgentFiles");
}

/// <summary>
/// Replaces the legacy hard-coded rule <c>if (lobID == "2100" &amp;&amp; SSOName == "HRA")</c> pick the
/// minor/adult assessment by the member's age — the LOB, age limit, and assessment names are now
/// configuration (legacy keys <c>SSOAssessmentSettings:Enabled/MinorAssessment/AdultAssessment</c>).
/// </summary>
public sealed class HraAssessmentOptions
{
    public bool Enabled { get; init; } = true;

    /// <summary>LOB the age-based assessment rule applies to.</summary>
    public string Lob { get; init; } = "2100";

    public string SsoName { get; init; } = SsoNames.Hra;

    /// <summary>Members younger than this get <see cref="MinorAssessmentName"/>.</summary>
    public int MinorAgeLimit { get; init; } = 18;

    public string? MinorAssessmentName { get; init; }

    public string? AdultAssessmentName { get; init; }
}
