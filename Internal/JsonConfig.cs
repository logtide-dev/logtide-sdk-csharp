using System.Text.Json;
using System.Text.Json.Serialization;

namespace LogWard.SDK.Internal;

/// <summary>
/// JSON serialization options for the SDK.
/// </summary>
internal static class JsonConfig
{
    /// <summary>
    /// Default JSON serializer options.
    /// </summary>
    public static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = false
    };
}
