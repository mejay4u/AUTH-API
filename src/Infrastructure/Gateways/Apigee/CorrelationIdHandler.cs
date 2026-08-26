using Microsoft.Extensions.Options;

namespace AuthApi.Infrastructure.Gateways.Apigee;

/// <summary>
/// Guarantees every outbound call carries exactly one correlation id.
///
/// The gateway sets the id from the client's header (or mints one); this handler is the backstop that
/// removes any duplicate the allow-listed client headers may have introduced, so ARTS never sees two
/// conflicting values for the same call.
/// </summary>
public sealed class CorrelationIdHandler(IOptions<ApigeeInternalOptions> options) : DelegatingHandler
{
    private readonly ApigeeInternalOptions _options = options.Value;

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var header = _options.CorrelationIdHeaderName;

        if (request.Options.TryGetValue(CorrelationIdKey, out var correlationId) &&
            !string.IsNullOrWhiteSpace(correlationId))
        {
            request.Headers.Remove(header);
            request.Headers.TryAddWithoutValidation(header, correlationId);
        }

        return base.SendAsync(request, cancellationToken);
    }

    /// <summary>Carries the correlation id from the gateway to this handler without a header round trip.</summary>
    public static readonly HttpRequestOptionsKey<string> CorrelationIdKey = new("AuthApi.CorrelationId");
}
