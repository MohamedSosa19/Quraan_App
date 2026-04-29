using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Quraan.Domain.Common;

namespace Quraan.Api.Middleware;

/// <summary>
/// Global RFC 7807 exception handler (Principle VII). Translates uncaught
/// exceptions into <see cref="ProblemDetails"/> with the request's
/// correlationId attached, never leaking stack traces in production.
/// </summary>
public sealed class ProblemDetailsExceptionHandler : IExceptionHandler
{
    private readonly ILogger<ProblemDetailsExceptionHandler> _log;
    private readonly IHostEnvironment _env;

    public ProblemDetailsExceptionHandler(ILogger<ProblemDetailsExceptionHandler> log, IHostEnvironment env)
    {
        _log = log;
        _env = env;
    }

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var correlationId = httpContext.Items.TryGetValue(CorrelationIdMiddleware.ItemsKey, out var v) ? v?.ToString() : null;
        _log.LogError(exception, "Unhandled exception (correlationId={CorrelationId})", correlationId);

        var problem = new ProblemDetails
        {
            Type = "https://quraan.app/problems/internal-error",
            Title = "An unexpected error occurred.",
            Status = StatusCodes.Status500InternalServerError,
            Instance = httpContext.Request.Path,
        };
        if (correlationId is not null) problem.Extensions["correlationId"] = correlationId;
        if (_env.IsDevelopment()) problem.Detail = exception.Message;

        httpContext.Response.StatusCode = problem.Status.Value;
        httpContext.Response.ContentType = "application/problem+json";
        await httpContext.Response.WriteAsJsonAsync(problem, cancellationToken).ConfigureAwait(false);
        return true;
    }
}
