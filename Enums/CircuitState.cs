namespace LogWard.SDK.Enums;

/// <summary>
/// Circuit breaker states for fault tolerance.
/// </summary>
public enum CircuitState
{
    /// <summary>
    /// Circuit is closed - normal operation, requests allowed.
    /// </summary>
    Closed,
    
    /// <summary>
    /// Circuit is open - requests blocked due to failures.
    /// </summary>
    Open,
    
    /// <summary>
    /// Circuit is half-open - test request allowed to check recovery.
    /// </summary>
    HalfOpen
}
