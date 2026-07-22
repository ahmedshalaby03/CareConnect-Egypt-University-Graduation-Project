using CareConnect.Application.Common.Models;
using Microsoft.AspNetCore.Diagnostics;

namespace CareConnect.Api.Middleware;

/// <summary>
/// Catches anything that escapes a controller and returns the standard envelope. Details of
/// the exception are logged, never sent to the client.
/// </summary>
public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;
    private readonly IHostEnvironment _environment;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger, IHostEnvironment environment)
    {
        _logger = logger;
        _environment = environment;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        _logger.LogError(
            exception,
            "Unhandled exception while processing {Method} {Path}.",
            httpContext.Request.Method,
            httpContext.Request.Path);

        var (statusCode, message) = exception switch
        {
            UnauthorizedAccessException => (StatusCodes.Status401Unauthorized, "You are not authorized to perform this action."),
            KeyNotFoundException => (StatusCodes.Status404NotFound, "The requested resource was not found."),
            _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred. Please try again later.")
        };

        // Stack traces are useful while developing and dangerous in production.
        var errors = _environment.IsDevelopment()
            ? new[] { exception.Message }
            : null;

        httpContext.Response.StatusCode = statusCode;
        httpContext.Response.ContentType = "application/json";

        await httpContext.Response.WriteAsJsonAsync(
            ApiResponse.Fail(message, errors),
            cancellationToken);

        return true;
    }
}
