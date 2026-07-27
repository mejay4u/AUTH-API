using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;

namespace Registration.Api.Infrastructure;

/// <summary>
/// Verifies the HMAC signature on inbound Descope calls to the <c>/descope</c> endpoints (machine-to-
/// machine). Checks a signed timestamp for replay protection, then compares an HMAC-SHA256 over
/// "timestamp.body" against the signature header using a constant-time comparison. Requests that fail
/// are rejected with 401 before reaching the handler. Runs as middleware (not an endpoint filter) so it
/// can read the raw body before model binding consumes it.
///
/// NOTE: the signed-payload construction and header names are configurable — align them with your
/// Descope webhook signature scheme.
/// </summary>
public sealed class DescopeSignatureMiddleware(
    RequestDelegate next,
    IOptions<DescopeOptions> options,
    ILogger<DescopeSignatureMiddleware> logger)
{
    private const string PathPrefix = "/api/v1/registration/descope";
    private readonly DescopeOptions _options = options.Value;

    public async Task InvokeAsync(HttpContext context)
    {
        if (!_options.Enabled || !context.Request.Path.StartsWithSegments(PathPrefix, StringComparison.OrdinalIgnoreCase))
        {
            await next(context);
            return;
        }

        var signature = context.Request.Headers[_options.SignatureHeader].ToString();
        var timestamp = context.Request.Headers[_options.TimestampHeader].ToString();

        if (string.IsNullOrEmpty(signature) || string.IsNullOrEmpty(timestamp) || !IsFreshTimestamp(timestamp))
        {
            await RejectAsync(context);
            return;
        }

        context.Request.EnableBuffering();
        string body;
        using (var reader = new StreamReader(context.Request.Body, Encoding.UTF8, leaveOpen: true))
        {
            body = await reader.ReadToEndAsync();
        }
        context.Request.Body.Position = 0;

        if (!SignatureMatches(timestamp, body, signature))
        {
            await RejectAsync(context);
            return;
        }

        await next(context);
    }

    private bool IsFreshTimestamp(string timestamp)
    {
        if (!long.TryParse(timestamp, out var unixSeconds))
        {
            return false;
        }

        var skew = DateTimeOffset.UtcNow - DateTimeOffset.FromUnixTimeSeconds(unixSeconds);
        return Math.Abs(skew.TotalSeconds) <= _options.TimestampToleranceSeconds;
    }

    private bool SignatureMatches(string timestamp, string body, string providedSignature)
    {
        var key = Encoding.UTF8.GetBytes(_options.SigningSecret);
        var computed = HMACSHA256.HashData(key, Encoding.UTF8.GetBytes($"{timestamp}.{body}"));
        var computedHex = Convert.ToHexString(computed).ToLowerInvariant();

        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(providedSignature.Trim().ToLowerInvariant()),
            Encoding.UTF8.GetBytes(computedHex));
    }

    private async Task RejectAsync(HttpContext context)
    {
        logger.LogWarning("Rejected Descope call to {Path}: invalid signature.", context.Request.Path);
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        await context.Response.WriteAsJsonAsync(new
        {
            title = "Invalid signature",
            status = StatusCodes.Status401Unauthorized
        });
    }
}

public static class DescopeSignatureMiddlewareExtensions
{
    public static IApplicationBuilder UseDescopeSignatureVerification(this IApplicationBuilder app) =>
        app.UseMiddleware<DescopeSignatureMiddleware>();
}
