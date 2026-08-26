namespace AuthApi.Application.Common.Models;

/// <summary>
/// One authentication call on its way upstream. The body is carried as the raw bytes the client sent
/// rather than a re-serialized DTO: the BFA is a relay, so a field the Member Portal and ARTS both
/// understand must survive the hop even when the BFA has never heard of it. That also means adding a
/// field to the login contract needs no BFA release.
/// </summary>
/// <param name="Body">The verbatim request body received from the client.</param>
/// <param name="MediaType">Media type of <paramref name="Body"/>, without parameters (e.g. "application/json").</param>
/// <param name="Headers">Client headers that survived the API layer's deny-list; the gateway applies its own allow-list.</param>
/// <param name="ClientIpAddress">The caller's IP, appended to X-Forwarded-For so ARTS still sees the true origin.</param>
/// <param name="CorrelationId">Correlation id for this call, echoed to the client and sent upstream.</param>
public sealed record UpstreamAuthRequest(
    string Body,
    string MediaType,
    IReadOnlyDictionary<string, string> Headers,
    string? ClientIpAddress,
    string CorrelationId);

/// <summary>
/// What ARTS answered, captured verbatim so the API layer can replay it to the client. The status code
/// is upstream's — a 401 from ARTS reaches the Member Portal as a 401, with ARTS's own body.
/// </summary>
/// <param name="StatusCode">Upstream HTTP status code, relayed unchanged.</param>
/// <param name="Body">Upstream response body, or <c>null</c> when there was none.</param>
/// <param name="MediaType">Upstream content type, or <c>null</c> when there was no body.</param>
/// <param name="Headers">Allow-listed upstream headers to copy onto the downstream response.</param>
/// <param name="CorrelationId">The correlation id this call was made under.</param>
public sealed record UpstreamAuthResponse(
    int StatusCode,
    string? Body,
    string? MediaType,
    IReadOnlyDictionary<string, string> Headers,
    string CorrelationId);
