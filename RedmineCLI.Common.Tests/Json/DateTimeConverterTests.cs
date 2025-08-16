using System.Text.Json;

using RedmineCLI.Common.Json;

using Xunit;

namespace RedmineCLI.Common.Tests.Json;

public class DateTimeConverterTests
{
    private readonly JsonSerializerOptions _options;

    public DateTimeConverterTests()
    {
        _options = new JsonSerializerOptions();
        _options.Converters.Add(new DateTimeConverter());
    }

    [Theory]
    [InlineData("\"2024-01-15T10:30:45Z\"", 2024, 1, 15, 10, 30, 45)]
    [InlineData("\"2024-01-15T10:30:45+09:00\"", 2024, 1, 15, 1, 30, 45)] // UTC conversion
    [InlineData("\"2024-01-15 10:30:45\"", 2024, 1, 15, 10, 30, 45)]
    [InlineData("\"2024-01-15\"", 2024, 1, 15, 0, 0, 0)]
    public void Read_ValidFormats_ParsesCorrectly(string json, int year, int month, int day, int hour, int minute, int second)
    {
        // Act
        var result = JsonSerializer.Deserialize<DateTime>(json, _options);

        // Assert
        Assert.Equal(year, result.Year);
        Assert.Equal(month, result.Month);
        Assert.Equal(day, result.Day);
        Assert.Equal(hour, result.Hour);
        Assert.Equal(minute, result.Minute);
        Assert.Equal(second, result.Second);
        Assert.Equal(DateTimeKind.Utc, result.Kind);
    }

    [Fact]
    public void Read_EmptyString_ReturnsMinValue()
    {
        // Arrange
        var json = "\"\"";

        // Act
        var result = JsonSerializer.Deserialize<DateTime>(json, _options);

        // Assert
        Assert.Equal(DateTime.MinValue, result);
    }

    [Fact]
    public void Read_NullString_ReturnsMinValue()
    {
        // Arrange
        var json = "null";

        // Act
        var result = JsonSerializer.Deserialize<DateTime?>(json, _options);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void Write_DateTime_FormatsCorrectly()
    {
        // Arrange
        var dateTime = new DateTime(2024, 1, 15, 10, 30, 45, DateTimeKind.Utc);

        // Act
        var json = JsonSerializer.Serialize(dateTime, _options);

        // Assert
        Assert.Equal("\"2024-01-15T10:30:45Z\"", json);
    }

    [Fact]
    public void Write_MinValue_FormatsCorrectly()
    {
        // Arrange
        var dateTime = DateTime.MinValue;

        // Act
        var json = JsonSerializer.Serialize(dateTime, _options);

        // Assert
        Assert.Equal("\"0001-01-01T00:00:00Z\"", json);
    }

    [Theory]
    [InlineData("2024-01-15T10:30:45.123Z")]
    [InlineData("2024/01/15 10:30:45")]
    public void Read_ParseableFormats_FallsBackToDefaultParsing(string dateString)
    {
        // Arrange
        var json = $"\"{dateString}\"";

        // Act & Assert
        var result = JsonSerializer.Deserialize<DateTime>(json, _options);
        Assert.NotEqual(DateTime.MinValue, result);
    }

    [Fact]
    public void Read_InvalidFormat_ThrowsException()
    {
        // Arrange
        var json = "\"invalid-date-format\"";

        // Act & Assert
        Assert.Throws<FormatException>(() => JsonSerializer.Deserialize<DateTime>(json, _options));
    }

    [Fact]
    public void RoundTrip_DateTime_PreservesValue()
    {
        // Arrange
        var original = new DateTime(2024, 1, 15, 10, 30, 45, DateTimeKind.Utc);

        // Act
        var json = JsonSerializer.Serialize(original, _options);
        var deserialized = JsonSerializer.Deserialize<DateTime>(json, _options);

        // Assert
        Assert.Equal(original, deserialized);
    }
}
