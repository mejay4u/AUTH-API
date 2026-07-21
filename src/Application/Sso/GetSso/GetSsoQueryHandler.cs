using AuthApi.Application.Common.Interfaces;
using AuthApi.Domain.Common;
using AuthApi.Domain.Sso;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AuthApi.Application.Sso.GetSso;

/// <summary>
/// The GetSso use case. Orchestration only — configuration loading is behind
/// <see cref="ISsoConfigurationRepository"/> (cached), and each integration's URL generation is an
/// <see cref="ISsoUrlProvider"/> strategy, so this handler stays the same size as integrations come and go.
/// </summary>
public sealed class GetSsoQueryHandler(
    ISsoConfigurationRepository configurationRepository,
    IEnumerable<ISsoUrlProvider> urlProviders,
    IOptions<SsoOptions> options,
    IDateTimeProvider clock,
    ILogger<GetSsoQueryHandler> logger)
    : IRequestHandler<GetSsoQuery, Result<SsoResult>>
{
    public async Task<Result<SsoResult>> Handle(GetSsoQuery request, CancellationToken cancellationToken)
    {
        var settings = options.Value;

        var allForLob = await configurationRepository.GetForLobAsync(request.Lob, request.PlanCode, cancellationToken);

        var configurations = allForLob
            .Where(c => c.SsoName.Equals(request.SsoName, StringComparison.OrdinalIgnoreCase))
            .ToArray();

        if (configurations.Length == 0)
        {
            return SsoErrors.NotConfigured;
        }

        var assessmentName = ResolveAssessmentName(request, settings.HraAssessment);
        if (assessmentName is not null)
        {
            configurations[0] = configurations[0] with { AssessmentName = assessmentName };
        }

        var skipped = settings.SkipLobs.Contains(request.Lob, StringComparer.OrdinalIgnoreCase);
        string? ssoUrl = null;

        if (!skipped)
        {
            var provider = urlProviders.FirstOrDefault(
                p => p.SsoName.Equals(request.SsoName, StringComparison.OrdinalIgnoreCase));

            if (provider is null)
            {
                // Legacy parity: an unknown SSO name fell through the switch's default case and the
                // configuration was returned without a URL.
                logger.LogWarning("No SSO URL provider registered for {SsoName}; returning configuration only.", request.SsoName);
            }
            else
            {
                var context = new SsoUrlContext(request.Lob, request.PlanCode, provider.SsoName, request.Member, configurations);
                ssoUrl = await provider.GetSsoUrlAsync(context, cancellationToken);
            }
        }

        // Legacy parity: the generated URL replaces the configured return URL on the first row, and a
        // skipped LOB gets it blanked so clients don't redirect to a stale configured value.
        if (!string.IsNullOrEmpty(ssoUrl) || skipped)
        {
            configurations[0] = configurations[0] with { PingFedReturnUrl = ssoUrl ?? string.Empty };
        }

        return new SsoResult(
            request.Lob,
            configurations[0].SsoName,
            configurations[0].AssessmentName,
            ssoUrl,
            request.Member,
            configurations);
    }

    /// <summary>
    /// Age-based HRA assessment selection (legacy hard-coded LOB 2100 rule, now configuration).
    /// Returns null when the rule doesn't apply or the member's date of birth is unknown.
    /// </summary>
    private string? ResolveAssessmentName(GetSsoQuery request, HraAssessmentOptions hra)
    {
        if (!hra.Enabled ||
            !request.Lob.Equals(hra.Lob, StringComparison.OrdinalIgnoreCase) ||
            !request.SsoName.Equals(hra.SsoName, StringComparison.OrdinalIgnoreCase) ||
            request.Member.DateOfBirth is not { } dateOfBirth)
        {
            return null;
        }

        var today = DateOnly.FromDateTime(clock.UtcNow);
        var age = today.Year - dateOfBirth.Year;
        if (dateOfBirth > today.AddYears(-age))
        {
            age--;
        }

        return age < hra.MinorAgeLimit ? hra.MinorAssessmentName : hra.AdultAssessmentName;
    }
}
