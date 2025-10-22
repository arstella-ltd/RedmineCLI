using System.Text.Json.Serialization;

namespace RedmineCLI.Models.Mcp;

/// <summary>
/// MCPツール定義
/// </summary>
public class Tool
{
    [JsonPropertyName("name")]
    public required string Name { get; set; }

    [JsonPropertyName("description")]
    public required string Description { get; set; }

    [JsonPropertyName("inputSchema")]
    public required InputSchema InputSchema { get; set; }
}

/// <summary>
/// ツールの入力スキーマ
/// </summary>
public class InputSchema
{
    [JsonPropertyName("type")]
    public required string Type { get; set; }

    [JsonPropertyName("properties")]
    public Dictionary<string, SchemaProperty> Properties { get; set; } = new();

    [JsonPropertyName("required")]
    public string[]? Required { get; set; }
}

/// <summary>
/// スキーマプロパティ
/// </summary>
public class SchemaProperty
{
    [JsonPropertyName("type")]
    public required string Type { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }
}

/// <summary>
/// tools/list レスポンス
/// </summary>
public class ToolsListResult
{
    [JsonPropertyName("tools")]
    public Tool[] Tools { get; set; } = Array.Empty<Tool>();
}
