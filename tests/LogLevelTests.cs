using Xunit;
using LogWard.SDK.Enums;

namespace LogWard.SDK.Tests;

public class LogLevelTests
{
    [Theory]
    [InlineData(LogLevel.Debug, "debug")]
    [InlineData(LogLevel.Info, "info")]
    [InlineData(LogLevel.Warn, "warn")]
    [InlineData(LogLevel.Error, "error")]
    [InlineData(LogLevel.Critical, "critical")]
    public void ToApiString_ReturnsCorrectValue(LogLevel level, string expected)
    {
        // Act
        var result = level.ToApiString();

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("debug", LogLevel.Debug)]
    [InlineData("DEBUG", LogLevel.Debug)]
    [InlineData("info", LogLevel.Info)]
    [InlineData("INFO", LogLevel.Info)]
    [InlineData("warn", LogLevel.Warn)]
    [InlineData("warning", LogLevel.Warn)]
    [InlineData("error", LogLevel.Error)]
    [InlineData("critical", LogLevel.Critical)]
    [InlineData("fatal", LogLevel.Critical)]
    public void FromString_ParsesCorrectly(string input, LogLevel expected)
    {
        // Act
        var result = LogLevelExtensions.FromString(input);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("unknown")]
    [InlineData("")]
    [InlineData(null)]
    public void FromString_ReturnsInfoForUnknown(string? input)
    {
        // Act
        var result = LogLevelExtensions.FromString(input!);

        // Assert
        Assert.Equal(LogLevel.Info, result);
    }
}
