using System.Text.Json;

using RedmineCLI.Common.Json;

using Xunit;

namespace RedmineCLI.Common.Tests.Json;

public class JsonSerializerOptionsProviderTests
{
    [Fact]
    public void GetDefaultOptions_ReturnsCorrectSettings()
    {
        // Act
        var options = JsonSerializerOptionsProvider.GetDefaultOptions();

        // Assert
        Assert.NotNull(options);
        Assert.Equal(JsonNamingPolicy.SnakeCaseLower, options.PropertyNamingPolicy);
        Assert.Equal(System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull, options.DefaultIgnoreCondition);
        Assert.False(options.WriteIndented);
        Assert.Contains(options.Converters, c => c is DateTimeConverter);
    }

    [Fact]
    public void GetPrettyPrintOptions_ReturnsIndentedSettings()
    {
        // Act
        var options = JsonSerializerOptionsProvider.GetPrettyPrintOptions();

        // Assert
        Assert.NotNull(options);
        Assert.Equal(JsonNamingPolicy.SnakeCaseLower, options.PropertyNamingPolicy);
        Assert.Equal(System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull, options.DefaultIgnoreCondition);
        Assert.True(options.WriteIndented);
        Assert.Contains(options.Converters, c => c is DateTimeConverter);
    }

    [Fact]
    public void GetDefaultOptions_ReturnsNewInstanceEachTime()
    {
        // Act
        var options1 = JsonSerializerOptionsProvider.GetDefaultOptions();
        var options2 = JsonSerializerOptionsProvider.GetDefaultOptions();

        // Assert
        Assert.NotSame(options1, options2);
    }

    [Fact]
    public void GetPrettyPrintOptions_ReturnsNewInstanceEachTime()
    {
        // Act
        var options1 = JsonSerializerOptionsProvider.GetPrettyPrintOptions();
        var options2 = JsonSerializerOptionsProvider.GetPrettyPrintOptions();

        // Assert
        Assert.NotSame(options1, options2);
    }

    [Fact]
    public void DefaultOptions_SerializesSnakeCase()
    {
        // Arrange
        var options = JsonSerializerOptionsProvider.GetDefaultOptions();
        var obj = new TestObject { FirstName = "John", LastName = "Doe" };

        // Act
        var json = JsonSerializer.Serialize(obj, options);

        // Assert
        Assert.Contains("\"first_name\":", json);
        Assert.Contains("\"last_name\":", json);
    }

    [Fact]
    public void DefaultOptions_IgnoresNullValues()
    {
        // Arrange
        var options = JsonSerializerOptionsProvider.GetDefaultOptions();
        var obj = new TestObject { FirstName = "John", LastName = null };

        // Act
        var json = JsonSerializer.Serialize(obj, options);

        // Assert
        Assert.Contains("\"first_name\":", json);
        Assert.DoesNotContain("\"last_name\":", json);
    }

    [Fact]
    public void PrettyPrintOptions_ProducesIndentedJson()
    {
        // Arrange
        var options = JsonSerializerOptionsProvider.GetPrettyPrintOptions();
        var obj = new TestObject { FirstName = "John", LastName = "Doe" };

        // Act
        var json = JsonSerializer.Serialize(obj, options);

        // Assert
        Assert.Contains("\n", json);
        Assert.Contains("  ", json); // Contains indentation
    }

    private class TestObject
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
    }
}
