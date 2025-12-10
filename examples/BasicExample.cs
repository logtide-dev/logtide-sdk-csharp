using LogWard.SDK;
using LogWard.SDK.Models;

// Basic usage example

Console.WriteLine("LogWard SDK - Basic Example");
Console.WriteLine("===========================\n");

// Create client
var client = new LogWardClient(new ClientOptions
{
    ApiUrl = "http://localhost:8080",
    ApiKey = "lp_your_api_key_here",
    Debug = true
});

// Basic logging
client.Debug("example", "This is a debug message");
client.Info("example", "Application started");
client.Warn("example", "This is a warning");

// Logging with metadata
client.Info("example", "User logged in", new Dictionary<string, object?>
{
    ["userId"] = 12345,
    ["email"] = "user@example.com",
    ["role"] = "admin"
});

// Error logging with exception
try
{
    throw new InvalidOperationException("Something went wrong!");
}
catch (Exception ex)
{
    client.Error("example", "An error occurred", ex);
}

// Critical logging
client.Critical("example", "System is shutting down", new Dictionary<string, object?>
{
    ["reason"] = "maintenance",
    ["scheduled"] = true
});

Console.WriteLine("\nWaiting for logs to be sent...");
await Task.Delay(2000);

// Get metrics
var metrics = client.GetMetrics();
Console.WriteLine($"\n--- Metrics ---");
Console.WriteLine($"Logs sent: {metrics.LogsSent}");
Console.WriteLine($"Logs dropped: {metrics.LogsDropped}");
Console.WriteLine($"Errors: {metrics.Errors}");
Console.WriteLine($"Circuit breaker state: {client.GetCircuitBreakerState()}");

// Cleanup
await client.DisposeAsync();
Console.WriteLine("\nClient disposed. Done!");
