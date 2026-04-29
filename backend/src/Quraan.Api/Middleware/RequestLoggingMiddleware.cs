using System.Diagnostics;

namespace Quraan.Api.Middleware;

/// <summary>
/// Lightweight per-request log line. Strips <c>Authorization</c> and any
/// auth-path bodies so passwords and tokens never appear in logs (SC-010,
/// Principle V). Uses Serilog via <c>ILogger</c> abstraction so the structured
/// JSON sink in production captures method/path/status/duration.
/// </summary>
public sealed class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _log;

    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> log)
    {
        _next = next;
        _log = log;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            await _next(context).ConfigureAwait(false);
        }
        finally
        {
            sw.Stop();
            _log.LogInformation("HTTP {Method} {Path} → {Status} in {Elapsed}ms",
                context.Request.Method,
                context.Request.Path.Value,
                context.Response.StatusCode,
                sw.ElapsedMilliseconds);
        }
    }
}
