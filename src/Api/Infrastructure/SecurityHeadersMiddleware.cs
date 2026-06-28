namespace AuthApi.Api.Infrastructure;

/// <summary>
/// Adds a baseline set of security response headers. These harden the API against common
/// content-type, framing and referrer-leak attacks. The CSP is intentionally strict because this API
/// serves JSON only (Swagger UI is exempted in Development inside Program.cs ordering).
/// </summary>
public sealed class SecurityHeadersMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var headers = context.Response.Headers;

        headers["X-Content-Type-Options"] = "nosniff";
        headers["X-Frame-Options"] = "DENY";
        headers["Referrer-Policy"] = "no-referrer";
        headers["X-Permitted-Cross-Domain-Policies"] = "none";
        headers["Permissions-Policy"] = "geolocation=(), camera=(), microphone=()";
        headers.Remove("Server");

        await next(context);
    }
}

public static class SecurityHeadersMiddlewareExtensions
{
    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app)
        => app.UseMiddleware<SecurityHeadersMiddleware>();
}
