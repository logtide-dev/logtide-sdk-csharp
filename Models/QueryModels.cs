using LogWard.SDK.Enums;

namespace LogWard.SDK.Models;

/// <summary>
/// Options for querying logs.
/// </summary>
public class QueryOptions
{
    /// <summary>
    /// Filter by service name.
    /// </summary>
    public string? Service { get; set; }

    /// <summary>
    /// Filter by log level.
    /// </summary>
    public LogLevel? Level { get; set; }

    /// <summary>
    /// Start time for the query range.
    /// </summary>
    public DateTime? From { get; set; }

    /// <summary>
    /// End time for the query range.
    /// </summary>
    public DateTime? To { get; set; }

    /// <summary>
    /// Full-text search query.
    /// </summary>
    public string? Query { get; set; }

    /// <summary>
    /// Maximum number of results to return. Default: 100.
    /// </summary>
    public int Limit { get; set; } = 100;

    /// <summary>
    /// Number of results to skip. Default: 0.
    /// </summary>
    public int Offset { get; set; } = 0;
}

/// <summary>
/// Response from a logs query.
/// </summary>
public class LogsResponse
{
    /// <summary>
    /// List of log entries.
    /// </summary>
    public List<LogEntry> Logs { get; set; } = new();

    /// <summary>
    /// Total number of logs matching the query.
    /// </summary>
    public int Total { get; set; }

    /// <summary>
    /// Maximum number of results returned.
    /// </summary>
    public int Limit { get; set; }

    /// <summary>
    /// Number of results skipped.
    /// </summary>
    public int Offset { get; set; }
}

/// <summary>
/// Options for aggregated statistics.
/// </summary>
public class AggregatedStatsOptions
{
    /// <summary>
    /// Start time for the aggregation range.
    /// </summary>
    public DateTime From { get; set; }

    /// <summary>
    /// End time for the aggregation range.
    /// </summary>
    public DateTime To { get; set; }

    /// <summary>
    /// Time interval for bucketing. Valid values: "1m", "5m", "1h", "1d". Default: "1h".
    /// </summary>
    public string Interval { get; set; } = "1h";

    /// <summary>
    /// Optional service filter.
    /// </summary>
    public string? Service { get; set; }
}

/// <summary>
/// Response from aggregated statistics query.
/// </summary>
public class AggregatedStatsResponse
{
    /// <summary>
    /// Time-bucketed log counts.
    /// </summary>
    public List<TimeSeriesEntry> Timeseries { get; set; } = new();

    /// <summary>
    /// Top services by log count.
    /// </summary>
    public List<ServiceCount> TopServices { get; set; } = new();

    /// <summary>
    /// Most common error messages.
    /// </summary>
    public List<ErrorCount> TopErrors { get; set; } = new();
}

/// <summary>
/// Single time series entry.
/// </summary>
public class TimeSeriesEntry
{
    /// <summary>
    /// Time bucket (ISO 8601).
    /// </summary>
    public string Bucket { get; set; } = string.Empty;

    /// <summary>
    /// Total logs in this bucket.
    /// </summary>
    public int Total { get; set; }

    /// <summary>
    /// Breakdown by log level.
    /// </summary>
    public Dictionary<string, int> ByLevel { get; set; } = new();
}

/// <summary>
/// Service with log count.
/// </summary>
public class ServiceCount
{
    /// <summary>
    /// Service name.
    /// </summary>
    public string Service { get; set; } = string.Empty;

    /// <summary>
    /// Number of logs.
    /// </summary>
    public int Count { get; set; }
}

/// <summary>
/// Error message with count.
/// </summary>
public class ErrorCount
{
    /// <summary>
    /// Error message.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Number of occurrences.
    /// </summary>
    public int Count { get; set; }
}
