using System.Text.Json.Serialization;

namespace RedmineCLI.Models.Mcp;

/// <summary>
/// tools/call レスポンス
/// </summary>
public class ToolCallResult
{
    [JsonPropertyName("content")]
    public TextContent[] Content { get; set; } = Array.Empty<TextContent>();
}

/// <summary>
/// テキストコンテンツ
/// </summary>
public class TextContent
{
    [JsonPropertyName("type")]
    public required string Type { get; set; }

    [JsonPropertyName("text")]
    public required string Text { get; set; }
}
