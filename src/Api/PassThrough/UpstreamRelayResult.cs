using AuthApi.Application.Common.Models;

namespace AuthApi.Api.PassThrough;

/// <summary>
/// Writes an <see cref="UpstreamAuthResponse"/> back to the client as-is: upstream's status code,
/// upstream's body, upstream's allow-listed headers.
///
/// A custom <see cref="IResult"/> rather than <c>Results.Content(...)</c> because the relay has to
/// reproduce a status code the framework has no helper for (ARTS may answer 423 Locked, 429, or
/// anything else) while writing the body byte-for-byte, without a serializer re-shaping it.
/// </summary>
internal sealed class UpstreamRelayResult(UpstreamAuthResponse response) : IResult
{
    public async Task ExecuteAsync(HttpContext httpContext)
    {
        httpContext.Response.StatusCode = response.StatusCode;

        foreach (var (name, value) in response.Headers)
        {
            // Never let upstream dictate framing headers — Kestrel owns those for this connection.
            if (IgnoredResponseHeaders.Contains(name))
            {
                continue;
            }

            httpContext.Response.Headers[name] = value;
        }

        if (string.IsNullOrEmpty(response.Body))
        {
            return;
        }

        httpContext.Response.ContentType = response.MediaType ?? "application/json";
        await httpContext.Response.WriteAsync(response.Body, httpContext.RequestAborted);
    }

    private static readonly HashSet<string> IgnoredResponseHeaders = new(StringComparer.OrdinalIgnoreCase)
    {
        "Content-Length",
        "Transfer-Encoding",
        "Connection",
        "Keep-Alive",
        "Upgrade",
        "Proxy-Authenticate",
        "Trailer",
        "TE"
    };
}
