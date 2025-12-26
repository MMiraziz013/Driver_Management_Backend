using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Driver_Management.Middlewares;

public class LoggingMiddleware : IMiddleware
{
    private readonly ILogger<LoggingMiddleware> _logger;

    public LoggingMiddleware(ILogger<LoggingMiddleware> logger)
    {
        _logger = logger;
    }
    
    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        var request = context.Request;
        _logger.LogInformation("-> Incoming request: {method} {path}", request.Method, request.Path);

        var start = DateTime.UtcNow;

        try
        {
            await next(context);
            var duration = DateTime.UtcNow - start;
            _logger.LogInformation("<- Outgoing response: {statusCode} in {duration}ms",
                context.Response.StatusCode, duration.TotalMilliseconds);
        }
        catch (Exception ex)
        {
            var duration = DateTime.UtcNow - start;
            _logger.LogError(ex, "!! Exception while processing {method} {path} after {duration}ms",
                request.Method, request.Path, duration.TotalMilliseconds);
            throw;
        }
    }
}