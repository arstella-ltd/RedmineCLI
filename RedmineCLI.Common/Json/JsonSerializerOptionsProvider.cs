using System.Text.Json;
using System.Text.Json.Serialization;

namespace RedmineCLI.Common.Json;

/// <summary>
/// Provides standardized JSON serializer options for Redmine API communication
/// </summary>
public static class JsonSerializerOptionsProvider
{
    /// <summary>
    /// Gets the default JSON serializer options for Redmine API
    /// </summary>
    public static JsonSerializerOptions GetDefaultOptions()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            WriteIndented = false,
            Converters = { new DateTimeConverter() }
        };

        return options;
    }

    /// <summary>
    /// Gets JSON serializer options for pretty-printed output
    /// </summary>
    public static JsonSerializerOptions GetPrettyPrintOptions()
    {
        var options = GetDefaultOptions();
        options.WriteIndented = true;
        return options;
    }
}
