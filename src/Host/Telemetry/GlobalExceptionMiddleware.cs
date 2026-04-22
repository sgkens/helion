namespace Helion.Host.Telemetry;

using System;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Helion.Host.Telemetry;

public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly LogDispatcher _logger;

    public GlobalExceptionMiddleware(RequestDelegate next, LogDispatcher logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        string correlationId = Guid.NewGuid().ToString("N");
        context.Response.Headers.Append("X-Helion-Correlation-Id", correlationId);

        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError("Kestrel Pipeline Unhandled", ex, correlationId);
            await HandleExceptionAsync(context, correlationId);
        }
    }

    private static Task HandleExceptionAsync(HttpContext context, string correlationId)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;

        var result = JsonSerializer.Serialize(new
        {
            error = "Internal Server Error",
            message = "An unexpected error occurred processing your request.",
            correlation_id = correlationId
        });

        return context.Response.WriteAsync(result);
    }
}
