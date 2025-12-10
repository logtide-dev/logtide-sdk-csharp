using Xunit;
using LogWard.SDK.Models;
using LogWard.SDK.Enums;

namespace LogWard.SDK.Tests;

public class ModelsTests
{
    [Fact]
    public void LogEntry_DefaultTime_SetsCurrentTime()
    {
        // Arrange & Act
        var entry = new LogEntry
        {
            Service = "test",
            Level = LogLevel.Info,
            Message = "Test message"
        };

        // Assert
        Assert.NotNull(entry.Time);
        Assert.True(DateTime.TryParse(entry.Time, out _));
    }

    [Fact]
    public void LogEntry_DefaultMetadata_IsEmpty()
    {
        // Arrange & Act
        var entry = new LogEntry
        {
            Service = "test",
            Level = LogLevel.Info,
            Message = "Test message"
        };

        // Assert
        Assert.NotNull(entry.Metadata);
        Assert.Empty(entry.Metadata);
    }

    [Fact]
    public void ClientOptions_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var options = new ClientOptions
        {
            ApiUrl = "http://localhost",
            ApiKey = "lp_test"
        };

        // Assert
        Assert.Equal(100, options.BatchSize);
        Assert.Equal(5000, options.FlushIntervalMs);
        Assert.Equal(10000, options.MaxBufferSize);
        Assert.Equal(3, options.MaxRetries);
        Assert.Equal(1000, options.RetryDelayMs);
        Assert.Equal(5, options.CircuitBreakerThreshold);
        Assert.Equal(30000, options.CircuitBreakerResetMs);
        Assert.True(options.EnableMetrics);
        Assert.False(options.Debug);
        Assert.False(options.AutoTraceId);
        Assert.Equal(30, options.HttpTimeoutSeconds);
        Assert.NotNull(options.GlobalMetadata);
        Assert.Empty(options.GlobalMetadata);
    }

    [Fact]
    public void QueryOptions_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var options = new QueryOptions();

        // Assert
        Assert.Equal(100, options.Limit);
        Assert.Equal(0, options.Offset);
        Assert.Null(options.Service);
        Assert.Null(options.Level);
        Assert.Null(options.Query);
        Assert.Null(options.From);
        Assert.Null(options.To);
    }

    [Fact]
    public void AggregatedStatsOptions_DefaultInterval_Is1Hour()
    {
        // Arrange & Act
        var options = new AggregatedStatsOptions
        {
            From = DateTime.UtcNow.AddDays(-1),
            To = DateTime.UtcNow
        };

        // Assert
        Assert.Equal("1h", options.Interval);
    }

    [Fact]
    public void ClientMetrics_Clone_CreatesIndependentCopy()
    {
        // Arrange
        var original = new ClientMetrics
        {
            LogsSent = 100,
            LogsDropped = 5,
            Errors = 2,
            Retries = 3,
            AvgLatencyMs = 45.5,
            CircuitBreakerTrips = 1
        };

        // Act
        var clone = original.Clone();
        original.LogsSent = 200;

        // Assert
        Assert.Equal(100, clone.LogsSent);
        Assert.Equal(5, clone.LogsDropped);
        Assert.Equal(2, clone.Errors);
        Assert.Equal(3, clone.Retries);
        Assert.Equal(45.5, clone.AvgLatencyMs);
        Assert.Equal(1, clone.CircuitBreakerTrips);
    }

    [Fact]
    public void SerializableLogEntry_FromLogEntry_ConvertsCorrectly()
    {
        // Arrange
        var entry = new LogEntry
        {
            Service = "test-service",
            Level = LogLevel.Error,
            Message = "Test message",
            Time = "2024-01-01T00:00:00Z",
            TraceId = "trace-123",
            Metadata = new Dictionary<string, object?>
            {
                ["key"] = "value"
            }
        };

        // Act
        var serializable = SerializableLogEntry.FromLogEntry(entry);

        // Assert
        Assert.Equal("test-service", serializable.Service);
        Assert.Equal("error", serializable.Level);
        Assert.Equal("Test message", serializable.Message);
        Assert.Equal("2024-01-01T00:00:00Z", serializable.Time);
        Assert.Equal("trace-123", serializable.TraceId);
        Assert.NotNull(serializable.Metadata);
        Assert.Equal("value", serializable.Metadata["key"]);
    }

    [Fact]
    public void SerializableLogEntry_FromLogEntry_NullsEmptyMetadata()
    {
        // Arrange
        var entry = new LogEntry
        {
            Service = "test",
            Level = LogLevel.Info,
            Message = "Test",
            Metadata = new Dictionary<string, object?>()
        };

        // Act
        var serializable = SerializableLogEntry.FromLogEntry(entry);

        // Assert
        Assert.Null(serializable.Metadata);
    }
}
