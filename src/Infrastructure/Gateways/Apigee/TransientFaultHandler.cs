using System.Net;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AuthApi.Infrastructure.Gateways.Apigee;

/// <summary>
/// Retries upstream calls that provably never reached ARTS.
///
/// Login and refresh are not idempotent — a retried login can burn a lockout counter, and a retried
/// refresh can consume a rotating token — so this handler is far more conservative than a stock
/// transient-fault policy:
///
/// <list type="bullet">
/// <item>Connection-level failures retry: the request never made it onto the wire.</item>
/// <item>502 / 503 from the proxy retry: Apigee is telling us it did not reach ARTS.</item>
/// <item><b>Timeouts do NOT retry.</b> A timeout means we stopped waiting, not that ARTS stopped
/// working; the call may well have succeeded, and replaying it risks rotating a refresh token the
/// client never received.</item>
/// <item>504 does NOT retry, for the same reason — the gateway timed out on a request it did forward.</item>
/// </list>
///
/// Backoff is exponential with full jitter, so a brief Apigee blip does not turn a burst of logins
/// into a synchronised retry storm against ARTS.
/// </summary>
public sealed class TransientFaultHandler(
    IOptions<ApigeeInternalOptions> options,
    ILogger<TransientFaultHandler> logger) : DelegatingHandler
{
    private readonly ApigeeInternalOptions _options = options.Value;

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var maxAttempts = _options.MaxRetryAttempts + 1;

        for (var attempt = 1; ; attempt++)
        {
            var isLastAttempt = attempt >= maxAttempts;

            // A HttpRequestMessage cannot be sent twice, so each attempt gets its own copy. Cloning
            // before the send (not after a failure) keeps the content stream readable.
            var attemptRequest = isLastAttempt ? request : await CloneAsync(request, cancellationToken);

            try
            {
                var response = await base.SendAsync(attemptRequest, cancellationToken);

                if (isLastAttempt || !IsRetryableStatus(response.StatusCode))
                {
                    return response;
                }

                logger.LogWarning(
                    "Upstream auth call returned {StatusCode}; retrying (attempt {Attempt} of {MaxAttempts}).",
                    (int)response.StatusCode, attempt, maxAttempts);

                response.Dispose();
            }
            catch (HttpRequestException ex) when (!isLastAttempt && !cancellationToken.IsCancellationRequested)
            {
                logger.LogWarning(
                    ex,
                    "Upstream auth call could not connect; retrying (attempt {Attempt} of {MaxAttempts}).",
                    attempt, maxAttempts);
            }
            finally
            {
                if (!isLastAttempt)
                {
                    attemptRequest.Dispose();
                }
            }

            await Task.Delay(BackoffFor(attempt), cancellationToken);
        }
    }

    /// <summary>Only statuses that mean "the proxy never delivered this" are safe to replay.</summary>
    private static bool IsRetryableStatus(HttpStatusCode status) =>
        status is HttpStatusCode.BadGateway or HttpStatusCode.ServiceUnavailable;

    private TimeSpan BackoffFor(int attempt)
    {
        var baseDelay = _options.RetryBaseDelayMilliseconds * Math.Pow(2, attempt - 1);

        // Full jitter: a uniform pick in [0, baseDelay] rather than a fixed delay every caller shares.
        return TimeSpan.FromMilliseconds(Random.Shared.NextDouble() * baseDelay);
    }

    private static async Task<HttpRequestMessage> CloneAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var clone = new HttpRequestMessage(request.Method, request.RequestUri) { Version = request.Version };

        if (request.Content is not null)
        {
            var body = await request.Content.ReadAsByteArrayAsync(cancellationToken);
            var content = new ByteArrayContent(body);

            foreach (var header in request.Content.Headers)
            {
                content.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }

            clone.Content = content;
        }

        foreach (var header in request.Headers)
        {
            clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        foreach (var option in (IDictionary<string, object?>)request.Options)
        {
            ((IDictionary<string, object?>)clone.Options)[option.Key] = option.Value;
        }

        return clone;
    }
}
