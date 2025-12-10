using LogWard.SDK;
using LogWard.SDK.Models;
using LogWard.SDK.Enums;

// Advanced usage example with all features

Console.WriteLine("LogWard SDK - Advanced Example");
Console.WriteLine("==============================\n");

// Create client with full configuration
var client = new LogWardClient(new ClientOptions
{
    ApiUrl = "http://localhost:8080",
    ApiKey = "lp_your_api_key_here",
    
    // Batching
    BatchSize = 50,
    FlushIntervalMs = 3000,
    
    // Buffer management
    MaxBufferSize = 5000,
    
    // Retry logic
    MaxRetries = 3,
    RetryDelayMs = 500,
    
    // Circuit breaker
    CircuitBreakerThreshold = 3,
    CircuitBreakerResetMs = 10000,
    
    // Options
    EnableMetrics = true,
    Debug = true,
    
    // Global metadata added to every log
    GlobalMetadata = new Dictionary<string, object?>
    {
        ["environment"] = "development",
        ["version"] = "1.0.0",
        ["machine"] = Environment.MachineName
    }
});

// 1. Basic logging with levels
Console.WriteLine("1. Basic Logging");
client.Debug("advanced", "Debug message");
client.Info("advanced", "Info message");
client.Warn("advanced", "Warning message");
client.Error("advanced", "Error message");
client.Critical("advanced", "Critical message");

// 2. Trace ID context
Console.WriteLine("\n2. Trace ID Context");

// Manual trace ID
client.SetTraceId("trace-001");
client.Info("advanced", "Log with manual trace ID");
client.Info("advanced", "Another log with same trace");
client.SetTraceId(null);

// Scoped trace ID
client.WithTraceId("trace-002", () =>
{
    client.Info("advanced", "Log inside scoped trace");
    client.Info("advanced", "Another log in scope");
});

// Auto-generated trace ID
client.WithNewTraceId(() =>
{
    client.Info("advanced", "Log with auto-generated trace ID");
    Console.WriteLine($"  Generated trace ID: {client.GetTraceId()}");
});

// 3. Custom log entries
Console.WriteLine("\n3. Custom Log Entries");
client.Log(new LogEntry
{
    Service = "custom-service",
    Level = LogLevel.Info,
    Message = "Custom log entry",
    Metadata = new Dictionary<string, object?>
    {
        ["custom"] = "data",
        ["nested"] = new Dictionary<string, object?>
        {
            ["key"] = "value"
        }
    }
});

// 4. Error serialization
Console.WriteLine("\n4. Error Serialization");
try
{
    try
    {
        throw new InvalidOperationException("Inner exception");
    }
    catch (Exception inner)
    {
        throw new ApplicationException("Outer exception", inner);
    }
}
catch (Exception ex)
{
    client.Error("advanced", "Nested exception example", ex);
}

// 5. Metrics
Console.WriteLine("\n5. Checking Metrics");
var metrics = client.GetMetrics();
Console.WriteLine($"  Logs sent: {metrics.LogsSent}");
Console.WriteLine($"  Logs dropped: {metrics.LogsDropped}");
Console.WriteLine($"  Errors: {metrics.Errors}");
Console.WriteLine($"  Retries: {metrics.Retries}");
Console.WriteLine($"  Avg latency: {metrics.AvgLatencyMs:F2}ms");
Console.WriteLine($"  Circuit breaker trips: {metrics.CircuitBreakerTrips}");
Console.WriteLine($"  Circuit state: {client.GetCircuitBreakerState()}");

// 6. Manual flush
Console.WriteLine("\n6. Manual Flush");
await client.FlushAsync();
Console.WriteLine("  Flush completed");

// 7. Updated metrics after flush
metrics = client.GetMetrics();
Console.WriteLine($"\n7. Updated Metrics");
Console.WriteLine($"  Logs sent: {metrics.LogsSent}");

// 8. Cleanup
Console.WriteLine("\n8. Cleanup");
await client.DisposeAsync();
Console.WriteLine("  Client disposed successfully");
