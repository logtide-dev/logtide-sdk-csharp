using System.Diagnostics;
using LogWard.SDK.Enums;
using LogWard.SDK.Models;
using Microsoft.AspNetCore.Http;

namespace LogWard.SDK.Middleware;

/// <summary>
/// Options for LogWard ASP.NET Core middleware.
/// </summary>
public class LogWardMiddlewareOptions
{
    /// <summary>
    /// LogWard client instance.
    /// </summary>
    public LogWardClient? Client { get; set; }

    /// <summary>
    /// Service name to use in logs.
    /// </summary>
    public string ServiceName { get; set; } = "aspnet-api";

    /// <summary>
    /// Whether to log incoming requests. Default: true.
    /// </summary>
    public bool LogRequests { get; set; } = true;

    /// <summary>
    /// Whether to log outgoing responses. Default: true.
    /// </summary>
    public bool LogResponses { get; set; } = true;

    /// <summary>
    /// Whether to log errors. Default: true.
    /// </summary>
    public bool LogErrors { get; set; } = true;

    /// <summary>
    /// Whether to include request headers in logs. Default: false.
    /// </summary>
    public bool IncludeHeaders { get; set; } = false;

    /// <summary>
    /// Whether to skip health check endpoints. Default: true.
    /// </summary>
    public bool SkipHealthCheck { get; set; } = true;

    /// <summary>
    /// Paths to skip logging for.
    /// </summary>
    public HashSet<string> SkipPaths { get; set; } = new();

    /// <summary>
    /// Header name to read/write trace ID. Default: "X-Trace-Id".
    /// </summary>
    public string TraceIdHeader { get; set; } = "X-Trace-Id";
}

/// <summary>
/// ASP.NET Core middleware for automatic HTTP request/response logging.
/// </summary>
public class LogWardMiddleware
{
    private readonly RequestDelegate _next;
    private readonly LogWardMiddlewareOptions _options;

    /// <summary>
    /// Creates a new LogWard middleware instance.
    /// </summary>
    public LogWardMiddleware(RequestDelegate next, LogWardMiddlewareOptions options)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        
        if (_options.Client == null)
            throw new ArgumentNullException(nameof(options), "LogWardMiddlewareOptions.Client cannot be null");
    }

    /// <summary>
    /// Invokes the middleware.
    /// </summary>
    public async Task InvokeAsync(HttpContext context)
    {
        // Check if path should be skipped
        if (ShouldSkip(context))
        {
            await _next(context);
            return;
        }

        // Get or generate trace ID
        var traceId = GetOrGenerateTraceId(context);
        _options.Client!.SetTraceId(traceId);

        // Add trace ID to response headers
        context.Response.OnStarting(() =>
        {
            context.Response.Headers[_options.TraceIdHeader] = traceId;
            return Task.CompletedTask;
        });

        var stopwatch = Stopwatch.StartNew();

        // Log request
        if (_options.LogRequests)
        {
            LogRequest(context);
        }

        try
        {
            await _next(context);
            stopwatch.Stop();

            // Log response
            if (_options.LogResponses)
            {
                LogResponse(context, stopwatch.ElapsedMilliseconds);
            }
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            // Log error
            if (_options.LogErrors)
            {
                LogError(context, ex, stopwatch.ElapsedMilliseconds);
            }

            throw;
        }
        finally
        {
            _options.Client!.SetTraceId(null);
        }
    }

    private bool ShouldSkip(HttpContext context)
    {
        var path = context.Request.Path.Value ?? "";

        // Skip health check endpoints
        if (_options.SkipHealthCheck)
        {
            var lowerPath = path.ToLowerInvariant();
            if (lowerPath.Contains("/health") || lowerPath == "/ready" || lowerPath == "/live")
            {
                return true;
            }
        }

        // Skip configured paths
        if (_options.SkipPaths.Contains(path))
        {
            return true;
        }

        return false;
    }

    private string GetOrGenerateTraceId(HttpContext context)
    {
        // Try to get trace ID from header
        if (context.Request.Headers.TryGetValue(_options.TraceIdHeader, out var existingTraceId) 
            && !string.IsNullOrEmpty(existingTraceId))
        {
            return existingTraceId!;
        }

        // Generate new trace ID
        return Guid.NewGuid().ToString();
    }

    private void LogRequest(HttpContext context)
    {
        var request = context.Request;
        var metadata = new Dictionary<string, object?>
        {
            ["method"] = request.Method,
            ["path"] = request.Path.Value,
            ["query"] = request.QueryString.Value,
            ["user_agent"] = request.Headers["User-Agent"].ToString(),
            ["remote_ip"] = context.Connection.RemoteIpAddress?.ToString()
        };

        if (_options.IncludeHeaders)
        {
            metadata["headers"] = request.Headers
                .Where(h => !h.Key.Equals("Authorization", StringComparison.OrdinalIgnoreCase))
                .ToDictionary(h => h.Key, h => h.Value.ToString());
        }

        _options.Client!.Log(new LogEntry
        {
            Service = _options.ServiceName,
            Level = LogLevel.Info,
            Message = $"{request.Method} {request.Path}",
            Metadata = metadata
        });
    }

    private void LogResponse(HttpContext context, long durationMs)
    {
        var statusCode = context.Response.StatusCode;
        var level = statusCode >= 500 ? LogLevel.Error
            : statusCode >= 400 ? LogLevel.Warn
            : LogLevel.Info;

        var metadata = new Dictionary<string, object?>
        {
            ["method"] = context.Request.Method,
            ["path"] = context.Request.Path.Value,
            ["status_code"] = statusCode,
            ["duration_ms"] = durationMs
        };

        _options.Client!.Log(new LogEntry
        {
            Service = _options.ServiceName,
            Level = level,
            Message = $"{context.Request.Method} {context.Request.Path} {statusCode} ({durationMs}ms)",
            Metadata = metadata
        });
    }

    private void LogError(HttpContext context, Exception exception, long durationMs)
    {
        var metadata = new Dictionary<string, object?>
        {
            ["method"] = context.Request.Method,
            ["path"] = context.Request.Path.Value,
            ["duration_ms"] = durationMs,
            ["error"] = new Dictionary<string, object?>
            {
                ["name"] = exception.GetType().Name,
                ["message"] = exception.Message,
                ["stack"] = exception.StackTrace
            }
        };

        _options.Client!.Log(new LogEntry
        {
            Service = _options.ServiceName,
            Level = LogLevel.Error,
            Message = $"Request error: {exception.Message}",
            Metadata = metadata
        });
    }
}
