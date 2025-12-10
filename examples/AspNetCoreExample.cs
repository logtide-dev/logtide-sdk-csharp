using LogWard.SDK;
using LogWard.SDK.Middleware;
using LogWard.SDK.Models;

// ASP.NET Core Minimal API example with LogWard middleware

var builder = WebApplication.CreateBuilder(args);

// Add LogWard client to DI container
builder.Services.AddLogWard(new ClientOptions
{
    ApiUrl = builder.Configuration["LogWard:ApiUrl"] ?? "http://localhost:8080",
    ApiKey = builder.Configuration["LogWard:ApiKey"] ?? "lp_your_api_key_here",
    Debug = builder.Environment.IsDevelopment(),
    GlobalMetadata = new Dictionary<string, object?>
    {
        ["environment"] = builder.Environment.EnvironmentName,
        ["version"] = "1.0.0"
    }
});

var app = builder.Build();

// Add LogWard middleware for automatic HTTP logging
app.UseLogWard(options =>
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
app.MapGet("/", (LogWardClient logger) =>
{
    logger.Info("aspnet-example", "Home page accessed");
    return Results.Ok(new { message = "Hello, World!" });
});

// Endpoint with custom logging
app.MapGet("/users/{id}", (int id, LogWardClient logger) =>
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
app.MapGet("/process", async (LogWardClient logger) =>
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
app.MapGet("/metrics", (LogWardClient logger) =>
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
    var logger = app.Services.GetRequiredService<LogWardClient>();
    await logger.FlushAsync();
    Console.WriteLine("Logs flushed on shutdown");
});

app.Run();
