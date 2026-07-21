using AuthApi.Application.Sso;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace AuthApi.Infrastructure.Sso;

/// <summary>
/// Caching decorator over the real repository (legacy behavior: 1-day sliding-expiration
/// <c>IMemoryCache</c> inside <c>FetchSSOData</c>, here separated so the data access stays testable).
/// The full per-LOB load is cached once and every SSO name is served from it.
/// </summary>
public sealed class CachedSsoConfigurationRepository(
    ISsoConfigurationRepository inner,
    IMemoryCache cache,
    IOptions<SsoOptions> options) : ISsoConfigurationRepository
{
    public async Task<IReadOnlyList<SsoConfigurationEntry>> GetForLobAsync(
        string lob, string? planCode, CancellationToken cancellationToken)
    {
        var key = $"sso-config:{lob}:{planCode}".ToUpperInvariant();

        if (cache.TryGetValue(key, out IReadOnlyList<SsoConfigurationEntry>? cached) && cached is not null)
        {
            return cached;
        }

        var entries = await inner.GetForLobAsync(lob, planCode, cancellationToken);

        // Only cache successful, non-empty loads — an empty result may be a transient data issue and
        // should not be pinned for a day (improvement over the legacy code, which cached whatever came back).
        if (entries.Count > 0)
        {
            cache.Set(key, entries, new MemoryCacheEntryOptions
            {
                SlidingExpiration = options.Value.ConfigCacheSlidingExpiration
            });
        }

        return entries;
    }
}
