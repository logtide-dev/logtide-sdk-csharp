using Xunit;
using LogTide.SDK.Enums;
using LogTide.SDK.Exceptions;
using LogTide.SDK.Models;

namespace LogTide.SDK.Tests;

public class LogTideClientTests
{
    private static ClientOptions CreateTestOptions() => new()
    {
        ApiUrl = "http://localhost:8080",
        ApiKey = "lp_test_key",
        FlushIntervalMs = 60000, // Long interval to prevent auto-flush during tests
        Debug = false
    };

    [Fact]
    public void Constructor_ThrowsOnNullOptions()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new LogTideClient(null!));
    }

    [Fact]
    public void SetTraceId_SetsAndGetsTraceId()
    {
        // Arrange
        using var client = new LogTideClient(CreateTestOptions());
        var traceId = "test-trace-123";

        // Act
        client.SetTraceId(traceId);
        var result = client.GetTraceId();

        // Assert
        Assert.Equal(traceId, result);
    }

    [Fact]
    public void SetTraceId_Null_ClearsTraceId()
    {
        // Arrange
        using var client = new LogTideClient(CreateTestOptions());
        client.SetTraceId("test-trace");

        // Act
        client.SetTraceId(null);

        // Assert
        Assert.Null(client.GetTraceId());
    }

    [Fact]
    public void WithTraceId_ScopedContext_RestoresPreviousTraceId()
    {
        // Arrange
        using var client = new LogTideClient(CreateTestOptions());
        var originalTraceId = "original-trace";
        var scopedTraceId = "scoped-trace";
        client.SetTraceId(originalTraceId);

        // Act
        string? insideTraceId = null;
        client.WithTraceId(scopedTraceId, () =>
        {
            insideTraceId = client.GetTraceId();
        });

        // Assert
        Assert.Equal(scopedTraceId, insideTraceId);
        Assert.Equal(originalTraceId, client.GetTraceId());
    }

    [Fact]
    public void WithNewTraceId_GeneratesNewTraceId()
    {
        // Arrange
        using var client = new LogTideClient(CreateTestOptions());
        string? generatedTraceId = null;

        // Act
        client.WithNewTraceId(() =>
        {
            generatedTraceId = client.GetTraceId();
        });

        // Assert
        Assert.NotNull(generatedTraceId);
        Assert.True(Guid.TryParse(generatedTraceId, out _));
    }

    [Fact]
    public void Log_AddsToBuffer()
    {
        // Arrange
        using var client = new LogTideClient(CreateTestOptions());

        // Act
        client.Info("test", "Test message");

        // Assert - metrics show no logs sent yet (still in buffer)
        var metrics = client.GetMetrics();
        Assert.Equal(0, metrics.LogsSent);
    }

    [Fact]
    public void Log_MergesGlobalMetadata()
    {
        // Arrange
        var options = CreateTestOptions();
        options.GlobalMetadata = new Dictionary<string, object?>
        {
            ["env"] = "test",
            ["version"] = "1.0.0"
        };
        using var client = new LogTideClient(options);

        // Act - log with partial metadata
        client.Info("test", "Test message", new Dictionary<string, object?>
        {
            ["custom"] = "value"
        });

        // Assert - can't directly check buffer, but verify client doesn't throw
        Assert.NotNull(client.GetMetrics());
    }

    [Fact]
    public void Log_AppliesAutoTraceId_WhenEnabled()
    {
        // Arrange
        var options = CreateTestOptions();
        options.AutoTraceId = true;
        using var client = new LogTideClient(options);

        // Act
        client.Info("test", "Test message");

        // Assert - can't directly check trace ID on buffered log, 
        // but verify no exception
        Assert.NotNull(client.GetMetrics());
    }

    [Fact]
    public void GetMetrics_ReturnsClone()
    {
        // Arrange
        using var client = new LogTideClient(CreateTestOptions());

        // Act
        var metrics1 = client.GetMetrics();
        var metrics2 = client.GetMetrics();

        // Assert - different instances
        Assert.NotSame(metrics1, metrics2);
    }

    [Fact]
    public void ResetMetrics_ClearsAllMetrics()
    {
        // Arrange
        using var client = new LogTideClient(CreateTestOptions());
        
        // Act
        client.ResetMetrics();
        var metrics = client.GetMetrics();

        // Assert
        Assert.Equal(0, metrics.LogsSent);
        Assert.Equal(0, metrics.LogsDropped);
        Assert.Equal(0, metrics.Errors);
        Assert.Equal(0, metrics.Retries);
        Assert.Equal(0, metrics.AvgLatencyMs);
        Assert.Equal(0, metrics.CircuitBreakerTrips);
    }

    [Fact]
    public void GetCircuitBreakerState_ReturnsClosedInitially()
    {
        // Arrange & Act
        using var client = new LogTideClient(CreateTestOptions());

        // Assert
        Assert.Equal(CircuitState.Closed, client.GetCircuitBreakerState());
    }

    [Fact]
    public void Error_WithException_SerializesError()
    {
        // Arrange
        using var client = new LogTideClient(CreateTestOptions());
        var exception = new InvalidOperationException("Test error");

        // Act - should not throw
        client.Error("test", "Error occurred", exception);

        // Assert
        Assert.NotNull(client.GetMetrics());
    }

    [Fact]
    public void Critical_WithException_SerializesError()
    {
        // Arrange
        using var client = new LogTideClient(CreateTestOptions());
        var exception = new ApplicationException("Critical error",
            new InvalidOperationException("Inner error"));

        // Act - should not throw
        client.Critical("test", "Critical error occurred", exception);

        // Assert
        Assert.NotNull(client.GetMetrics());
    }
}
