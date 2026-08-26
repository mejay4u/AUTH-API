using System.ComponentModel.DataAnnotations;

namespace AuthApi.Infrastructure.Gateways.Apigee;

/// <summary>
/// Everything the BFA needs to reach the ARTS auth service through Apigee Internal. Bound from the
/// <c>ApigeeInternal</c> configuration section; secrets (<see cref="ApiKey"/>,
/// <see cref="ClientCertificatePassword"/>) belong in Key Vault / OpenShift secrets, never in
/// appsettings.json.
/// </summary>
public sealed class ApigeeInternalOptions
{
    public const string SectionName = "ApigeeInternal";

    /// <summary>Absolute base address of the Apigee Internal proxy, e.g. <c>https://internal-apigee.corp/arts/v1</c>.</summary>
    [Required(AllowEmptyStrings = false)]
    public string BaseAddress { get; set; } = string.Empty;

    /// <summary>Path of the ARTS login operation, relative to <see cref="BaseAddress"/>.</summary>
    [Required(AllowEmptyStrings = false)]
    public string LoginPath { get; set; } = "auth/login";

    /// <summary>Path of the ARTS refresh operation, relative to <see cref="BaseAddress"/>.</summary>
    [Required(AllowEmptyStrings = false)]
    public string RefreshPath { get; set; } = "auth/refresh";

    /// <summary>
    /// Overall budget for one upstream call, including retries. Keep it comfortably below the timeout
    /// Apigee External applies to the BFA, so the client gets our 504 rather than the edge's.
    /// </summary>
    [Range(1, 120)]
    public int TimeoutSeconds { get; set; } = 15;

    /// <summary>Header carrying the Apigee API key/product credential. Set to empty to disable.</summary>
    public string ApiKeyHeaderName { get; set; } = "x-api-key";

    /// <summary>The Apigee API key. Inject from a secret store.</summary>
    public string? ApiKey { get; set; }

    /// <summary>Optional PKCS#12 client certificate for mutual TLS to Apigee Internal.</summary>
    public string? ClientCertificatePath { get; set; }

    /// <summary>Password for <see cref="ClientCertificatePath"/>. Inject from a secret store.</summary>
    public string? ClientCertificatePassword { get; set; }

    /// <summary>
    /// Retries for calls we can prove never reached ARTS (connection failures, and 502/503 from the
    /// proxy itself). Timeouts are deliberately NOT retried — see <c>TransientFaultHandler</c>.
    /// </summary>
    [Range(0, 5)]
    public int MaxRetryAttempts { get; set; } = 2;

    /// <summary>Base delay for the exponential backoff between retries.</summary>
    [Range(0, 5000)]
    public int RetryBaseDelayMilliseconds { get; set; } = 200;

    /// <summary>Largest upstream response body the BFA will buffer before relaying it.</summary>
    [Range(1024, 4 * 1024 * 1024)]
    public int MaxResponseBodyBytes { get; set; } = 256 * 1024;

    /// <summary>Header used to correlate one client call across BFA, Apigee Internal and ARTS logs.</summary>
    [Required(AllowEmptyStrings = false)]
    public string CorrelationIdHeaderName { get; set; } = "X-Correlation-Id";

    /// <summary>
    /// Client headers allowed to travel upstream. An allow-list, not a deny-list: the BFA is exposed
    /// to the internet through Apigee External, so anything not named here stops at the BFA.
    /// </summary>
    public string[] ForwardRequestHeaders { get; set; } =
    [
        "Accept-Language",
        "X-Correlation-Id",
        "X-Request-Id",
        "X-Device-Id",
        "X-Channel",
        "User-Agent"
    ];

    /// <summary>
    /// Upstream headers allowed back down to the client. Hop-by-hop and framing headers
    /// (Transfer-Encoding, Content-Length, Connection, Keep-Alive …) are never relayed — Kestrel
    /// owns those for the downstream connection.
    /// </summary>
    public string[] ForwardResponseHeaders { get; set; } =
    [
        "WWW-Authenticate",
        "Retry-After",
        "X-Correlation-Id",
        "X-Request-Id",
        "X-RateLimit-Limit",
        "X-RateLimit-Remaining",
        "X-RateLimit-Reset"
    ];
}
