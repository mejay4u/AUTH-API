using System.Net.Http.Headers;
using System.Text;
using AuthApi.Application.Common.Interfaces;
using AuthApi.Application.Common.Models;
using AuthApi.Domain.Common;
using AuthApi.Domain.Members;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AuthApi.Infrastructure.Gateways.Apigee;

/// <summary>
/// The Infrastructure adapter behind <see cref="IUpstreamAuthGateway"/>: a typed
/// <see cref="HttpClient"/> that posts the client's verbatim body to Apigee Internal, which fronts the
/// on-prem ARTS .NET auth service.
///
/// The contract with the layers above is narrow on purpose. A response — any response, including a
/// 401 — is returned as a success carrying upstream's status and body. A <see cref="Result"/> failure
/// means only that no answer could be obtained, and the two shapes that can take
/// (<see cref="UpstreamAuthErrors.Unavailable"/>, <see cref="UpstreamAuthErrors.Timeout"/>) are the
/// only transport concepts the Application layer ever sees.
/// </summary>
internal sealed class ApigeeInternalAuthGateway(
    HttpClient httpClient,
    IOptions<ApigeeInternalOptions> options,
    ILogger<ApigeeInternalAuthGateway> logger) : IUpstreamAuthGateway
{
    private readonly ApigeeInternalOptions _options = options.Value;

    public Task<Result<UpstreamAuthResponse>> LoginAsync(UpstreamAuthRequest request, CancellationToken cancellationToken) =>
        SendAsync(_options.LoginPath, request, cancellationToken);

    public Task<Result<UpstreamAuthResponse>> RefreshAsync(UpstreamAuthRequest request, CancellationToken cancellationToken) =>
        SendAsync(_options.RefreshPath, request, cancellationToken);

    private async Task<Result<UpstreamAuthResponse>> SendAsync(
        string path,
        UpstreamAuthRequest request,
        CancellationToken cancellationToken)
    {
        using var message = BuildRequest(path, request);

        try
        {
            using var response = await httpClient.SendAsync(
                message, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

            var body = await ReadBoundedBodyAsync(response, cancellationToken);

            return new UpstreamAuthResponse(
                (int)response.StatusCode,
                body,
                response.Content.Headers.ContentType?.MediaType,
                CollectResponseHeaders(response),
                request.CorrelationId);
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            // HttpClient surfaces its own timeout as a cancellation that our token did not request.
            logger.LogError(
                "Upstream auth call to {Path} timed out after {TimeoutSeconds}s ({CorrelationId}).",
                path, _options.TimeoutSeconds, request.CorrelationId);

            return UpstreamAuthErrors.Timeout;
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(
                ex,
                "Upstream auth call to {Path} could not reach Apigee Internal ({CorrelationId}).",
                path, request.CorrelationId);

            return UpstreamAuthErrors.Unavailable;
        }
    }

    private HttpRequestMessage BuildRequest(string path, UpstreamAuthRequest request)
    {
        var message = new HttpRequestMessage(HttpMethod.Post, path)
        {
            // The body goes upstream exactly as the client sent it — no deserialize/re-serialize round
            // trip that could drop a field ARTS knows about and the BFA does not.
            Content = new StringContent(request.Body, Encoding.UTF8, request.MediaType)
        };

        message.Options.Set(CorrelationIdHandler.CorrelationIdKey, request.CorrelationId);

        if (!string.IsNullOrWhiteSpace(_options.ApiKeyHeaderName) && !string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            message.Headers.TryAddWithoutValidation(_options.ApiKeyHeaderName, _options.ApiKey);
        }

        foreach (var name in _options.ForwardRequestHeaders)
        {
            if (request.Headers.TryGetValue(name, out var value) && !string.IsNullOrWhiteSpace(value))
            {
                message.Headers.TryAddWithoutValidation(name, value);
            }
        }

        // ARTS applies its own per-origin lockout, so it must see the caller's IP rather than the
        // BFA pod's. Append to any chain Apigee External already started.
        if (!string.IsNullOrWhiteSpace(request.ClientIpAddress))
        {
            var existing = request.Headers.TryGetValue(ForwardedForHeader, out var chain) && !string.IsNullOrWhiteSpace(chain)
                ? $"{chain}, {request.ClientIpAddress}"
                : request.ClientIpAddress;

            message.Headers.TryAddWithoutValidation(ForwardedForHeader, existing);
        }

        return message;
    }

    /// <summary>
    /// Buffers the upstream body up to <see cref="ApigeeInternalOptions.MaxResponseBodyBytes"/>. The
    /// cap is a guard, not a feature: a token response is small, and a malformed or hostile upstream
    /// should not be able to make the BFA allocate without bound.
    /// </summary>
    private async Task<string?> ReadBoundedBodyAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.Content.Headers.ContentLength is 0)
        {
            return null;
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);

        var buffer = new byte[Math.Min(_options.MaxResponseBodyBytes, 8 * 1024)];
        using var captured = new MemoryStream();

        int read;
        while ((read = await stream.ReadAsync(buffer, cancellationToken)) > 0)
        {
            if (captured.Length + read > _options.MaxResponseBodyBytes)
            {
                logger.LogError(
                    "Upstream auth response exceeded {MaxResponseBodyBytes} bytes and was discarded.",
                    _options.MaxResponseBodyBytes);

                throw new HttpRequestException("The upstream response body exceeded the configured limit.");
            }

            captured.Write(buffer, 0, read);
        }

        return captured.Length == 0
            ? null
            : DecodeBody(captured, response.Content.Headers.ContentType);
    }

    private static string DecodeBody(MemoryStream captured, MediaTypeHeaderValue? contentType)
    {
        var encoding = Encoding.UTF8;

        if (!string.IsNullOrWhiteSpace(contentType?.CharSet))
        {
            try
            {
                encoding = Encoding.GetEncoding(contentType.CharSet);
            }
            catch (ArgumentException)
            {
                // Unknown charset from upstream — UTF-8 is the right assumption for a JSON API.
            }
        }

        return encoding.GetString(captured.GetBuffer(), 0, (int)captured.Length);
    }

    private Dictionary<string, string> CollectResponseHeaders(HttpResponseMessage response)
    {
        var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var name in _options.ForwardResponseHeaders)
        {
            // A header can sit on either collection depending on whether it describes the message or
            // its content, so both are consulted.
            if (response.Headers.TryGetValues(name, out var values))
            {
                headers[name] = string.Join(", ", values);
            }
            else if (response.Content.Headers.TryGetValues(name, out var contentValues))
            {
                headers[name] = string.Join(", ", contentValues);
            }
        }

        return headers;
    }

    private const string ForwardedForHeader = "X-Forwarded-For";
}
