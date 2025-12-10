namespace LogWard.SDK.Enums;

/// <summary>
/// Log severity levels supported by LogWard.
/// </summary>
public enum LogLevel
{
    /// <summary>
    /// Debug level - detailed diagnostic information.
    /// </summary>
    Debug,
    
    /// <summary>
    /// Info level - general informational messages.
    /// </summary>
    Info,
    
    /// <summary>
    /// Warning level - potentially harmful situations.
    /// </summary>
    Warn,
    
    /// <summary>
    /// Error level - error events that might still allow the application to continue.
    /// </summary>
    Error,
    
    /// <summary>
    /// Critical level - severe error events that lead the application to abort.
    /// </summary>
    Critical
}

/// <summary>
/// Extension methods for LogLevel enum.
/// </summary>
public static class LogLevelExtensions
{
    /// <summary>
    /// Converts the LogLevel to its string representation for the API.
    /// </summary>
    public static string ToApiString(this LogLevel level) => level switch
    {
        LogLevel.Debug => "debug",
        LogLevel.Info => "info",
        LogLevel.Warn => "warn",
        LogLevel.Error => "error",
        LogLevel.Critical => "critical",
        _ => "info"
    };

    /// <summary>
    /// Parses a string to LogLevel.
    /// </summary>
    public static LogLevel FromString(string value) => value?.ToLowerInvariant() switch
    {
        "debug" => LogLevel.Debug,
        "info" => LogLevel.Info,
        "warn" or "warning" => LogLevel.Warn,
        "error" => LogLevel.Error,
        "critical" or "fatal" => LogLevel.Critical,
        _ => LogLevel.Info
    };
}
