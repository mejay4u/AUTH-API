using AuthApi.Application.Common.Configuration;
using Microsoft.Extensions.Configuration;

namespace AuthApi.Infrastructure;

/// <summary>
/// Reads the authentication mode straight from configuration, before the DI container exists.
///
/// The composition root has to branch on the mode while it is still *building* the container — which
/// services to register at all, which middleware to add, which endpoints to map — so it cannot wait
/// for <c>IOptions&lt;AuthOptions&gt;</c> to be resolvable.
/// </summary>
public static class AuthModeConfigurationExtensions
{
    public static AuthMode GetAuthMode(this IConfiguration configuration) =>
        configuration.GetSection(AuthOptions.SectionName).GetValue(nameof(AuthOptions.Mode), AuthMode.Local);

    /// <summary>True when this deployment is the ARO-hosted BFA relaying to on-prem ARTS.</summary>
    public static bool IsPassThrough(this IConfiguration configuration) =>
        configuration.GetAuthMode() == AuthMode.PassThrough;
}
