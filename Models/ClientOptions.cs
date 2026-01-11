namespace LogTide.SDK.Models;

/// <summary>
/// Configuration options for LogTideClient.
/// </summary>
public class ClientOptions
{
    /// <summary>
    /// Base URL of the LogTide API (e.g., "https://logward.dev" or "http://localhost:8080").
    /// </summary>
    public string ApiUrl { get; set; } = string.Empty;

    /// <summary>
    /// Project API key (starts with "lp_").
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Number of logs to batch before sending. Default: 100.
    /// </summary>
    public int BatchSize { get; set; } = 100;

    /// <summary>
    /// Interval in milliseconds to auto-flush logs. Default: 5000ms.
    /// </summary>
    public int FlushIntervalMs { get; set; } = 5000;

    /// <summary>
    /// Maximum logs in buffer (prevents memory leak). Default: 10000.
    /// </summary>
    public int MaxBufferSize { get; set; } = 10000;

    /// <summary>
    /// Maximum retry attempts on failure. Default: 3.
    /// </summary>
    public int MaxRetries { get; set; } = 3;

    /// <summary>
    /// Initial retry delay in milliseconds (uses exponential backoff). Default: 1000ms.
    /// </summary>
    public int RetryDelayMs { get; set; } = 1000;

    /// <summary>
    /// Number of consecutive failures before opening circuit breaker. Default: 5.
    /// </summary>
    public int CircuitBreakerThreshold { get; set; } = 5;

    /// <summary>
    /// Time in milliseconds before retrying after circuit opens. Default: 30000ms.
    /// </summary>
    public int CircuitBreakerResetMs { get; set; } = 30000;

    /// <summary>
    /// Whether to track internal metrics. Default: true.
    /// </summary>
    public bool EnableMetrics { get; set; } = true;

    /// <summary>
    /// Enable debug logging to console. Default: false.
    /// </summary>
    public bool Debug { get; set; } = false;

    /// <summary>
    /// Global metadata added to all logs.
    /// </summary>
    public Dictionary<string, object?> GlobalMetadata { get; set; } = new();

    /// <summary>
    /// Automatically generate trace IDs for logs that don't have one. Default: false.
    /// </summary>
    public bool AutoTraceId { get; set; } = false;

    /// <summary>
    /// HTTP timeout in seconds. Default: 30.
    /// </summary>
    public int HttpTimeoutSeconds { get; set; } = 30;
}
