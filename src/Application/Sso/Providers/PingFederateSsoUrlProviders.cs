namespace AuthApi.Application.Sso.Providers;

// The straightforward integrations: each legacy switch case that only forwarded to a PingFed method
// becomes a one-line strategy. CERTIFI and SDS, which build attribute sets, live in their own files.

public sealed class AbarcaSsoUrlProvider(IPingFederateService pingFed) : ISsoUrlProvider
{
    public string SsoName => SsoNames.Abarca;

    public Task<string?> GetSsoUrlAsync(SsoUrlContext context, CancellationToken cancellationToken) =>
        pingFed.GetAbarcaSsoAsync(context, cancellationToken);
}

public sealed class HraSsoUrlProvider(IPingFederateService pingFed) : ISsoUrlProvider
{
    public string SsoName => SsoNames.Hra;

    public Task<string?> GetSsoUrlAsync(SsoUrlContext context, CancellationToken cancellationToken) =>
        pingFed.GetHraJivaSsoAsync(context, cancellationToken);
}

public sealed class PlanOfCareSsoUrlProvider(IPingFederateService pingFed) : ISsoUrlProvider
{
    public string SsoName => SsoNames.PlanOfCare;

    public Task<string?> GetSsoUrlAsync(SsoUrlContext context, CancellationToken cancellationToken) =>
        pingFed.GetPlanOfCareSsoAsync(context, cancellationToken);
}

public sealed class ChatSsoUrlProvider(IPingFederateService pingFed) : ISsoUrlProvider
{
    public string SsoName => SsoNames.Chat;

    public Task<string?> GetSsoUrlAsync(SsoUrlContext context, CancellationToken cancellationToken) =>
        pingFed.GetChatSsoAsync(context, cancellationToken);
}

public sealed class SoftheonSsoUrlProvider(IPingFederateService pingFed) : ISsoUrlProvider
{
    public string SsoName => SsoNames.Softheon;

    public Task<string?> GetSsoUrlAsync(SsoUrlContext context, CancellationToken cancellationToken) =>
        pingFed.GetSoftheonSsoAsync(context, cancellationToken);
}
