# LogWard .NET SDK

Official .NET SDK for LogWard with advanced features: retry logic, circuit breaker, query API, distributed tracing, and ASP.NET Core middleware support.

## Features

- ✅ **Automatic batching** with configurable size and interval
- ✅ **Retry logic** with exponential backoff
- ✅ **Circuit breaker** pattern for fault tolerance
- ✅ **Max buffer size** with drop policy to prevent memory leaks
- ✅ **Query API** for searching and filtering logs
- ✅ **Trace ID context** for distributed tracing
- ✅ **Global metadata** added to all logs
- ✅ **Structured error serialization**
- ✅ **Internal metrics** (logs sent, errors, latency, etc.)
- ✅ **ASP.NET Core middleware** for auto-logging HTTP requests
- ✅ **Dependency injection support**
- ✅ **Full async/await support**
- ✅ **Thread-safe**

## Installation

```bash
dotnet add package LogWard.SDK
```

Or via Package Manager:

```powershell
Install-Package LogWard.SDK
```

## Quick Start

```csharp
using LogWard.SDK;
using LogWard.SDK.Models;

var client = new LogWardClient(new ClientOptions
{
    ApiUrl = "http://localhost:8080",
    ApiKey = "lp_your_api_key_here"
});

// Send logs
client.Info("api-gateway", "Server started", new() { ["port"] = 3000 });
client.Error("database", "Connection failed", new Exception("Timeout"));

// Graceful shutdown
await client.DisposeAsync();
```

---

## Configuration Options

### Basic Options

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| `ApiUrl` | `string` | **required** | Base URL of your LogWard instance |
| `ApiKey` | `string` | **required** | Project API key (starts with `lp_`) |
| `BatchSize` | `int` | `100` | Number of logs to batch before sending |
| `FlushIntervalMs` | `int` | `5000` | Interval in ms to auto-flush logs |

### Advanced Options

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| `MaxBufferSize` | `int` | `10000` | Max logs in buffer (prevents memory leak) |
| `MaxRetries` | `int` | `3` | Max retry attempts on failure |
| `RetryDelayMs` | `int` | `1000` | Initial retry delay (exponential backoff) |
| `CircuitBreakerThreshold` | `int` | `5` | Failures before opening circuit |
| `CircuitBreakerResetMs` | `int` | `30000` | Time before retrying after circuit opens |
| `EnableMetrics` | `bool` | `true` | Track internal metrics |
| `Debug` | `bool` | `false` | Enable debug logging to console |
| `GlobalMetadata` | `Dictionary` | `{}` | Metadata added to all logs |
| `AutoTraceId` | `bool` | `false` | Auto-generate trace IDs for logs |
| `HttpTimeoutSeconds` | `int` | `30` | HTTP request timeout |

### Example: Full Configuration

```csharp
var client = new LogWardClient(new ClientOptions
{
    ApiUrl = "http://localhost:8080",
    ApiKey = "lp_your_api_key_here",

    // Batching
    BatchSize = 100,
    FlushIntervalMs = 5000,

    // Buffer management
    MaxBufferSize = 10000,

    // Retry with exponential backoff (1s → 2s → 4s)
    MaxRetries = 3,
    RetryDelayMs = 1000,

    // Circuit breaker
    CircuitBreakerThreshold = 5,
    CircuitBreakerResetMs = 30000,

    // Metrics & debugging
    EnableMetrics = true,
    Debug = true,

    // Global context
    GlobalMetadata = new()
    {
        ["env"] = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"),
        ["version"] = "1.0.0",
        ["hostname"] = Environment.MachineName
    },

    // Auto trace IDs
    AutoTraceId = false
});
```

---

## Logging Methods

### Basic Logging

```csharp
client.Debug("service-name", "Debug message");
client.Info("service-name", "Info message", new() { ["userId"] = 123 });
client.Warn("service-name", "Warning message");
client.Error("service-name", "Error message", new() { ["custom"] = "data" });
client.Critical("service-name", "Critical message");
```

### Error Logging with Auto-Serialization

The SDK automatically serializes exceptions:

```csharp
try
{
    throw new InvalidOperationException("Database timeout");
}
catch (Exception ex)
{
    // Automatically serializes error with stack trace
    client.Error("database", "Query failed", ex);
}
```

Generated log metadata:
```json
{
  "error": {
    "name": "InvalidOperationException",
    "message": "Database timeout",
    "stack": "at Program.Main() in ..."
  }
}
```

### Custom Log Entry

```csharp
client.Log(new LogEntry
{
    Service = "custom-service",
    Level = LogLevel.Info,
    Message = "Custom log",
    Time = DateTime.UtcNow.ToString("O"),
    Metadata = new() { ["key"] = "value" },
    TraceId = "custom-trace-id"
});
```

---

## Trace ID Context

Track requests across services with trace IDs.

### Manual Trace ID

```csharp
client.SetTraceId("request-123");

client.Info("api", "Request received");
client.Info("database", "Querying users");
client.Info("api", "Response sent");

client.SetTraceId(null); // Clear context
```

### Scoped Trace ID

```csharp
client.WithTraceId("request-456", () =>
{
    client.Info("api", "Processing in context");
    client.Warn("cache", "Cache miss");
});
// Trace ID automatically restored after block
```

### Auto-Generated Trace ID

```csharp
client.WithNewTraceId(() =>
{
    client.Info("worker", "Background job started");
    client.Info("worker", "Job completed");
});
```

---

## Query API

Search and retrieve logs programmatically.

### Basic Query

```csharp
var result = await client.QueryAsync(new QueryOptions
{
    Service = "api-gateway",
    Level = LogLevel.Error,
    From = DateTime.UtcNow.AddDays(-1),
    To = DateTime.UtcNow,
    Limit = 100,
    Offset = 0
});

Console.WriteLine($"Found {result.Total} logs");
foreach (var log in result.Logs)
{
    Console.WriteLine($"{log.Time}: {log.Message}");
}
```

### Full-Text Search

```csharp
var result = await client.QueryAsync(new QueryOptions
{
    Query = "timeout",
    Limit = 50
});
```

### Get Logs by Trace ID

```csharp
var logs = await client.GetByTraceIdAsync("trace-123");
Console.WriteLine($"Trace has {logs.Count} logs");
```

### Aggregated Statistics

```csharp
var stats = await client.GetAggregatedStatsAsync(new AggregatedStatsOptions
{
    From = DateTime.UtcNow.AddDays(-7),
    To = DateTime.UtcNow,
    Interval = "1h", // "1m" | "5m" | "1h" | "1d"
    Service = "api-gateway" // Optional
});

Console.WriteLine("Time series:");
foreach (var entry in stats.Timeseries)
{
    Console.WriteLine($"  {entry.Bucket}: {entry.Total} logs");
}
```

---

## Metrics

Track SDK performance and health.

```csharp
var metrics = client.GetMetrics();

Console.WriteLine($"Logs sent: {metrics.LogsSent}");
Console.WriteLine($"Logs dropped: {metrics.LogsDropped}");
Console.WriteLine($"Errors: {metrics.Errors}");
Console.WriteLine($"Retries: {metrics.Retries}");
Console.WriteLine($"Avg latency: {metrics.AvgLatencyMs}ms");
Console.WriteLine($"Circuit breaker trips: {metrics.CircuitBreakerTrips}");

// Get circuit breaker state
Console.WriteLine($"Circuit state: {client.GetCircuitBreakerState()}"); // Closed | Open | HalfOpen

// Reset metrics
client.ResetMetrics();
```

---

## ASP.NET Core Integration

### Setup with Dependency Injection

**Program.cs:**

```csharp
using LogWard.SDK;
using LogWard.SDK.Middleware;
using LogWard.SDK.Models;

var builder = WebApplication.CreateBuilder(args);

// Add LogWard
builder.Services.AddLogWard(new ClientOptions
{
    ApiUrl = builder.Configuration["LogWard:ApiUrl"]!,
    ApiKey = builder.Configuration["LogWard:ApiKey"]!,
    GlobalMetadata = new()
    {
        ["env"] = builder.Environment.EnvironmentName
    }
});

var app = builder.Build();

// Add middleware for auto-logging HTTP requests
app.UseLogWard(options =>
{
    options.ServiceName = "my-api";
    options.LogRequests = true;
    options.LogResponses = true;
    options.LogErrors = true;
    options.SkipHealthCheck = true;
    options.SkipPaths.Add("/metrics");
});

app.MapGet("/", () => "Hello World!");

app.Run();
```

### Middleware Options

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| `ServiceName` | `string` | `"aspnet-api"` | Service name in logs |
| `LogRequests` | `bool` | `true` | Log incoming requests |
| `LogResponses` | `bool` | `true` | Log outgoing responses |
| `LogErrors` | `bool` | `true` | Log unhandled exceptions |
| `IncludeHeaders` | `bool` | `false` | Include request headers |
| `SkipHealthCheck` | `bool` | `true` | Skip /health endpoints |
| `SkipPaths` | `HashSet<string>` | `{}` | Paths to skip |
| `TraceIdHeader` | `string` | `"X-Trace-Id"` | Header for trace ID |

### Using LogWard in Controllers

```csharp
[ApiController]
[Route("[controller]")]
public class WeatherController : ControllerBase
{
    private readonly LogWardClient _logger;

    public WeatherController(LogWardClient logger)
    {
        _logger = logger;
    }

    [HttpGet]
    public IActionResult Get()
    {
        _logger.Info("weather-api", "Fetching weather data");
        
        try
        {
            // ... business logic
            return Ok(new { Temperature = 25 });
        }
        catch (Exception ex)
        {
            _logger.Error("weather-api", "Failed to fetch weather", ex);
            throw;
        }
    }
}
```

---

## Best Practices

### 1. Always Dispose on Shutdown

```csharp
var client = new LogWardClient(options);

// ... use client

// Dispose flushes remaining logs
await client.DisposeAsync();
```

Or with ASP.NET Core:

```csharp
var app = builder.Build();

app.Lifetime.ApplicationStopping.Register(async () =>
{
    var logger = app.Services.GetRequiredService<LogWardClient>();
    await logger.FlushAsync();
});
```

### 2. Use Global Metadata

```csharp
var client = new LogWardClient(new ClientOptions
{
    ApiUrl = "...",
    ApiKey = "...",
    GlobalMetadata = new()
    {
        ["env"] = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"),
        ["version"] = typeof(Program).Assembly.GetName().Version?.ToString(),
        ["machine"] = Environment.MachineName
    }
});
```

### 3. Enable Debug Mode in Development

```csharp
var client = new LogWardClient(new ClientOptions
{
    ApiUrl = "...",
    ApiKey = "...",
    Debug = builder.Environment.IsDevelopment()
});
```

### 4. Monitor Metrics in Production

```csharp
// Periodic health check
_ = Task.Run(async () =>
{
    while (true)
    {
        await Task.Delay(TimeSpan.FromMinutes(1));
        
        var metrics = client.GetMetrics();
        
        if (metrics.LogsDropped > 0)
        {
            Console.WriteLine($"Warning: {metrics.LogsDropped} logs dropped");
        }
        
        if (client.GetCircuitBreakerState() == CircuitState.Open)
        {
            Console.WriteLine("Error: Circuit breaker is OPEN!");
        }
    }
});
```

---

## API Reference

### LogWardClient

#### Constructor
```csharp
new LogWardClient(ClientOptions options)
new LogWardClient(ClientOptions options, HttpClient httpClient)
```

#### Logging Methods
- `Log(LogEntry entry)`
- `Debug(string service, string message, Dictionary<string, object?>? metadata = null)`
- `Info(string service, string message, Dictionary<string, object?>? metadata = null)`
- `Warn(string service, string message, Dictionary<string, object?>? metadata = null)`
- `Error(string service, string message, Dictionary<string, object?>? metadata = null)`
- `Error(string service, string message, Exception exception)`
- `Critical(string service, string message, Dictionary<string, object?>? metadata = null)`
- `Critical(string service, string message, Exception exception)`

#### Context Methods
- `SetTraceId(string? traceId)`
- `GetTraceId(): string?`
- `WithTraceId(string traceId, Action action)`
- `WithTraceId<T>(string traceId, Func<T> func)`
- `WithNewTraceId(Action action)`
- `WithNewTraceId<T>(Func<T> func)`

#### Query Methods
- `QueryAsync(QueryOptions options, CancellationToken ct = default): Task<LogsResponse>`
- `GetByTraceIdAsync(string traceId, CancellationToken ct = default): Task<List<LogEntry>>`
- `GetAggregatedStatsAsync(AggregatedStatsOptions options, CancellationToken ct = default): Task<AggregatedStatsResponse>`

#### Metrics
- `GetMetrics(): ClientMetrics`
- `ResetMetrics()`
- `GetCircuitBreakerState(): CircuitState`

#### Lifecycle
- `FlushAsync(CancellationToken ct = default): Task`
- `Dispose()`
- `DisposeAsync(): ValueTask`

---

## Supported Frameworks

- .NET 6.0
- .NET 7.0
- .NET 8.0

---

## License

MIT

---

## Contributing

Contributions are welcome! Please open an issue or PR on [GitHub](https://github.com/logward-dev/logward-sdk-csharp).

---

## Support

- **Documentation**: [https://logward.dev/docs](https://logward.dev/docs)
- **Issues**: [GitHub Issues](https://github.com/logward-dev/logward-sdk-csharp/issues)
