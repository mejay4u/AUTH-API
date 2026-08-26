using System.Text.Json;
using AuthApi.Application.Common.Configuration;
using AuthApi.Application.Common.Models;
using AuthApi.Domain.Common;
using AuthApi.Domain.Members;

namespace AuthApi.Api.PassThrough;

/// <summary>
/// Turns an inbound HTTP request into the transport-neutral <see cref="UpstreamAuthRequest"/> the
/// Application layer works with. This is the one place in the pass-through that touches
/// <see cref="HttpContext"/>; everything downstream of it is framework-free.
/// </summary>
internal static class PassThroughRequestReader
{
    /// <summary>
    /// Buffers the body (bounded by <see cref="AuthOptions.MaxRequestBodyBytes"/>) and collects the
    /// headers worth carrying. Returns a failure only for input the BFA cannot relay at all.
    /// </summary>
    public static async Task<Result<UpstreamAuthRequest>> ReadAsync(
        HttpContext httpContext,
        AuthOptions options,
        CancellationToken cancellationToken)
    {
        var body = await ReadBoundedBodyAsync(httpContext.Request, options.MaxRequestBodyBytes, cancellationToken);

        if (body is null)
        {
            return UpstreamAuthErrors.PayloadTooLarge;
        }

        return new UpstreamAuthRequest(
            body,
            ResolveMediaType(httpContext.Request.ContentType),
            CollectHeaders(httpContext.Request),
            httpContext.Connection.RemoteIpAddress?.ToString(),
            ResolveCorrelationId(httpContext));
    }

    /// <summary>
    /// Parses the buffered body just far enough to validate it, without ever materialising the
    /// credential. Callers get the non-secret fields plus a "was it present" flag for the secret ones.
    /// </summary>
    public static bool TryReadJsonFields(
        string body,
        out Dictionary<string, string?> fields)
    {
        fields = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

        try
        {
            using var document = JsonDocument.Parse(body);

            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                return false;
            }

            foreach (var property in document.RootElement.EnumerateObject())
            {
                fields[property.Name] = property.Value.ValueKind == JsonValueKind.String
                    ? property.Value.GetString()
                    : null;
            }

            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    /// <summary>
    /// Reads at most <paramref name="maxBytes"/>, returning <c>null</c> if the client sent more.
    /// Reading through a bounded loop rather than <c>ReadToEndAsync</c> means an oversized body is
    /// rejected while it arrives, not after the BFA has already buffered all of it.
    /// </summary>
    private static async Task<string?> ReadBoundedBodyAsync(
        HttpRequest request,
        int maxBytes,
        CancellationToken cancellationToken)
    {
        if (request.ContentLength > maxBytes)
        {
            return null;
        }

        var buffer = new byte[Math.Min(maxBytes, 8 * 1024)];
        using var captured = new MemoryStream();

        int read;
        while ((read = await request.Body.ReadAsync(buffer, cancellationToken)) > 0)
        {
            if (captured.Length + read > maxBytes)
            {
                return null;
            }

            captured.Write(buffer, 0, read);
        }

        return System.Text.Encoding.UTF8.GetString(captured.GetBuffer(), 0, (int)captured.Length);
    }

    /// <summary>
    /// Strips the media type's parameters. <c>StringContent</c> rejects a value like
    /// "application/json; charset=utf-8", and the charset is re-applied on the way out anyway.
    /// </summary>
    private static string ResolveMediaType(string? contentType)
    {
        if (string.IsNullOrWhiteSpace(contentType))
        {
            return DefaultMediaType;
        }

        var separator = contentType.IndexOf(';');
        var mediaType = (separator < 0 ? contentType : contentType[..separator]).Trim();

        return mediaType.Length == 0 ? DefaultMediaType : mediaType;
    }

    /// <summary>
    /// Honours a correlation id the caller (or Apigee External) already assigned so one id spans the
    /// whole hop chain; mints one otherwise. Caller-supplied values are length-capped and stripped of
    /// anything outside a conservative character set — this value ends up in log lines and in an
    /// outbound header, so it is treated as untrusted input.
    /// </summary>
    private static string ResolveCorrelationId(HttpContext httpContext)
    {
        foreach (var header in CorrelationHeaders)
        {
            var candidate = httpContext.Request.Headers[header].ToString();

            if (string.IsNullOrWhiteSpace(candidate))
            {
                continue;
            }

            var sanitized = new string(candidate
                .Where(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_')
                .Take(MaxCorrelationIdLength)
                .ToArray());

            if (sanitized.Length > 0)
            {
                return sanitized;
            }
        }

        return httpContext.TraceIdentifier;
    }

    /// <summary>
    /// Collects client headers minus a deny-list of things that must never travel. The gateway then
    /// applies its own allow-list, so a header has to clear both to reach ARTS.
    /// </summary>
    private static Dictionary<string, string> CollectHeaders(HttpRequest request)
    {
        var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var header in request.Headers)
        {
            if (DeniedRequestHeaders.Contains(header.Key))
            {
                continue;
            }

            headers[header.Key] = header.Value.ToString();
        }

        return headers;
    }

    private const string DefaultMediaType = "application/json";
    private const int MaxCorrelationIdLength = 64;

    private static readonly string[] CorrelationHeaders = ["X-Correlation-Id", "X-Request-Id"];

    private static readonly HashSet<string> DeniedRequestHeaders = new(StringComparer.OrdinalIgnoreCase)
    {
        // Framing and connection headers belong to the inbound hop, not the outbound one.
        "Host",
        "Content-Length",
        "Content-Type",
        "Transfer-Encoding",
        "Connection",
        "Keep-Alive",
        "Upgrade",
        "Expect",
        "TE",
        "Trailer",
        "Proxy-Authorization",

        // Credentials scoped to the edge. Apigee External's client credential must not be replayed to
        // Apigee Internal, which has its own; and the BFA attaches its own API key downstream.
        "Authorization",
        "Cookie",
        "x-api-key",
        "apikey"
    };
}
