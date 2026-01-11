using LogTide.SDK;
using LogTide.SDK.Middleware;
using LogTide.SDK.Models;

// ASP.NET Core Minimal API example with LogTide middleware

var builder = WebApplication.CreateBuilder(args);

// Add LogTide client to DI container
builder.Services.AddLogTide(new ClientOptions
{
    ApiUrl = builder.Configuration["LogTide:ApiUrl"] ?? "http://localhost:8080",
    ApiKey = builder.Configuration["LogTide:ApiKey"] ?? "lp_your_api_key_here",
    Debug = builder.Environment.IsDevelopment(),
    GlobalMetadata = new Dictionary<string, object?>
    {
        ["environment"] = builder.Environment.EnvironmentName,
        ["version"] = "1.0.0"
    }
});

var app = builder.Build();

// Add LogTide middleware for automatic HTTP logging
app.UseLogTide(options =>
{
    options.ServiceName = "aspnet-example";
    options.LogRequests = true;
    options.LogResponses = true;
    options.LogErrors = true;
    options.SkipHealthCheck = true;
    options.SkipPaths.Add("/favicon.ico");
});

// Health check endpoint (will be skipped by middleware)
app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

// Basic endpoint
app.MapGet("/", (LogTideClient logger) =>
{
    logger.Info("aspnet-example", "Home page accessed");
    return Results.Ok(new { message = "Hello, World!" });
});

// Endpoint with custom logging
app.MapGet("/users/{id}", (int id, LogTideClient logger) =>
{
    logger.Info("aspnet-example", $"Fetching user {id}", new Dictionary<string, object?>
    {
        ["userId"] = id
    });
    
    return Results.Ok(new
    {
        id,
        name = $"User {id}",
        email = $"user{id}@example.com"
    });
});

// Endpoint that throws an error (will be logged by middleware)
app.MapGet("/error", () =>
{
    throw new InvalidOperationException("This is a test error!");
});

// Endpoint with trace ID context
app.MapGet("/process", async (LogTideClient logger) =>
{
    await logger.WithTraceId(Guid.NewGuid().ToString(), async () =>
    {
        logger.Info("aspnet-example", "Starting process");
        
        await Task.Delay(100); // Simulate work
        logger.Debug("aspnet-example", "Step 1 completed");
        
        await Task.Delay(100);
        logger.Debug("aspnet-example", "Step 2 completed");
        
        await Task.Delay(100);
        logger.Info("aspnet-example", "Process completed");
    });
    
    return Results.Ok(new { status = "completed" });
});

// Metrics endpoint
app.MapGet("/metrics", (LogTideClient logger) =>
{
    var metrics = logger.GetMetrics();
    return Results.Ok(new
    {
        logsSent = metrics.LogsSent,
        logsDropped = metrics.LogsDropped,
        errors = metrics.Errors,
        retries = metrics.Retries,
        avgLatencyMs = metrics.AvgLatencyMs,
        circuitBreakerTrips = metrics.CircuitBreakerTrips,
        circuitBreakerState = logger.GetCircuitBreakerState().ToString()
    });
});

// Graceful shutdown
app.Lifetime.ApplicationStopping.Register(async () =>
{
    var logger = app.Services.GetRequiredService<LogTideClient>();
    await logger.FlushAsync();
    Console.WriteLine("Logs flushed on shutdown");
});

app.Run();
