namespace PsikoAl.Client.Security;

public sealed class SecurityHeadersMiddleware(RequestDelegate next, IHostEnvironment environment)
{
    public Task InvokeAsync(HttpContext context)
    {
        var headers = context.Response.Headers;
        headers.ContentSecurityPolicy = BuildContentSecurityPolicy(context.Request.Host.Value, environment.IsDevelopment());
        headers.XFrameOptions = "DENY";
        headers.XContentTypeOptions = "nosniff";
        headers["Referrer-Policy"] = "no-referrer";
        headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";
        return next(context);
    }

    public static string BuildContentSecurityPolicy(string? host, bool isDevelopment)
    {
        var connectSources = isDevelopment
            ? "'self' ws: wss:"
            : string.IsNullOrEmpty(host) ? "'self'" : $"'self' wss://{host}";

        return string.Join(
            "; ",
            "default-src 'self'",
            "script-src 'self'",
            "style-src 'self' 'unsafe-inline'",
            "img-src 'self' data:",
            "font-src 'self' data:",
            $"connect-src {connectSources}",
            "object-src 'none'",
            "base-uri 'self'",
            "form-action 'self'",
            "frame-ancestors 'none'");
    }
}
