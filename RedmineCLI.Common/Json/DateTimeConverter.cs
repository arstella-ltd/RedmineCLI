using System.Text.Json;
using System.Text.Json.Serialization;

namespace RedmineCLI.Common.Json;

/// <summary>
/// JSON converter for DateTime values with support for multiple date formats
/// </summary>
public class DateTimeConverter : JsonConverter<DateTime>
{
    private readonly string[] _formats = new[]
    {
        "yyyy-MM-dd'T'HH:mm:ss'Z'",
        "yyyy-MM-dd'T'HH:mm:sszzz",
        "yyyy-MM-dd HH:mm:ss",
        "yyyy-MM-dd"
    };

    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var dateString = reader.GetString();

        if (string.IsNullOrEmpty(dateString))
        {
            return DateTime.MinValue;
        }

        foreach (var format in _formats)
        {
            if (DateTime.TryParseExact(dateString, format,
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.AssumeUniversal | System.Globalization.DateTimeStyles.AdjustToUniversal,
                out var result))
            {
                return result;
            }
        }

        return DateTime.Parse(dateString);
    }

    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'"));
    }
}
