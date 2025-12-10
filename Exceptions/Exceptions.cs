namespace LogWard.SDK.Exceptions;

/// <summary>
/// Base exception for LogWard SDK errors.
/// </summary>
public class LogWardException : Exception
{
    /// <summary>
    /// Creates a new LogWardException.
    /// </summary>
    public LogWardException() : base() { }

    /// <summary>
    /// Creates a new LogWardException with a message.
    /// </summary>
    public LogWardException(string message) : base(message) { }

    /// <summary>
    /// Creates a new LogWardException with a message and inner exception.
    /// </summary>
    public LogWardException(string message, Exception innerException) 
        : base(message, innerException) { }
}

/// <summary>
/// Exception thrown when the log buffer is full.
/// </summary>
public class BufferFullException : LogWardException
{
    /// <summary>
    /// Creates a new BufferFullException.
    /// </summary>
    public BufferFullException() 
        : base("Log buffer is full. Logs are being dropped.") { }

    /// <summary>
    /// Creates a new BufferFullException with a custom message.
    /// </summary>
    public BufferFullException(string message) : base(message) { }
}

/// <summary>
/// Exception thrown when the circuit breaker is open.
/// </summary>
public class CircuitBreakerOpenException : LogWardException
{
    /// <summary>
    /// Creates a new CircuitBreakerOpenException.
    /// </summary>
    public CircuitBreakerOpenException() 
        : base("Circuit breaker is open. Requests are being blocked.") { }

    /// <summary>
    /// Creates a new CircuitBreakerOpenException with a custom message.
    /// </summary>
    public CircuitBreakerOpenException(string message) : base(message) { }
}

/// <summary>
/// Exception thrown when an API request fails.
/// </summary>
public class ApiException : LogWardException
{
    /// <summary>
    /// HTTP status code of the failed request.
    /// </summary>
    public int StatusCode { get; }

    /// <summary>
    /// Creates a new ApiException.
    /// </summary>
    public ApiException(int statusCode, string message) 
        : base($"API request failed with status {statusCode}: {message}")
    {
        StatusCode = statusCode;
    }

    /// <summary>
    /// Creates a new ApiException with an inner exception.
    /// </summary>
    public ApiException(int statusCode, string message, Exception innerException) 
        : base($"API request failed with status {statusCode}: {message}", innerException)
    {
        StatusCode = statusCode;
    }
}
