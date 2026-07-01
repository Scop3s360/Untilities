using System.Diagnostics;

namespace SaverSearch.Api.Middleware;

public class RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        var request = context.Request;

        logger.LogInformation("HTTP Request Started: {Method} {Path}", request.Method, request.Path);

        try
        {
            await next(context);
            stopwatch.Stop();
            
            logger.LogInformation(
                "HTTP Request Completed: {Method} {Path} responded {StatusCode} in {ElapsedMs}ms",
                request.Method,
                request.Path,
                context.Response.StatusCode,
                stopwatch.ElapsedMilliseconds
            );
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            logger.LogError(
                ex,
                "HTTP Request Failed: {Method} {Path} threw an exception after {ElapsedMs}ms",
                request.Method,
                request.Path,
                stopwatch.ElapsedMilliseconds
            );
            throw;
        }
    }
}
