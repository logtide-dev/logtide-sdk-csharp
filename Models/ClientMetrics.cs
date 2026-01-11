namespace LogTide.SDK.Models;

/// <summary>
/// Internal SDK metrics for monitoring.
/// </summary>
public class ClientMetrics
{
    /// <summary>
    /// Total logs successfully sent.
    /// </summary>
    public long LogsSent { get; set; }

    /// <summary>
    /// Logs dropped due to buffer overflow.
    /// </summary>
    public long LogsDropped { get; set; }

    /// <summary>
    /// Total number of send errors.
    /// </summary>
    public long Errors { get; set; }

    /// <summary>
    /// Total number of retry attempts.
    /// </summary>
    public long Retries { get; set; }

    /// <summary>
    /// Average send latency in milliseconds.
    /// </summary>
    public double AvgLatencyMs { get; set; }

    /// <summary>
    /// Number of times the circuit breaker has tripped.
    /// </summary>
    public long CircuitBreakerTrips { get; set; }

    /// <summary>
    /// Creates a copy of the metrics.
    /// </summary>
    public ClientMetrics Clone() => new()
    {
        LogsSent = LogsSent,
        LogsDropped = LogsDropped,
        Errors = Errors,
        Retries = Retries,
        AvgLatencyMs = AvgLatencyMs,
        CircuitBreakerTrips = CircuitBreakerTrips
    };
}
