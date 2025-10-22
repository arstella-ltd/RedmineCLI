using System.Text.Json.Serialization;

namespace RedmineCLI.Models.Mcp;

/// <summary>
/// MCPリソース定義
/// </summary>
public class Resource
{
    [JsonPropertyName("uri")]
    public required string Uri { get; set; }

    [JsonPropertyName("name")]
    public required string Name { get; set; }

    [JsonPropertyName("description")]
    public required string Description { get; set; }

    [JsonPropertyName("mimeType")]
    public required string MimeType { get; set; }
}

/// <summary>
/// resources/list レスポンス
/// </summary>
public class ResourcesListResult
{
    [JsonPropertyName("resources")]
    public Resource[] Resources { get; set; } = Array.Empty<Resource>();
}

/// <summary>
/// resources/read レスポンス
/// </summary>
public class ResourceReadResult
{
    [JsonPropertyName("contents")]
    public ResourceContent[] Contents { get; set; } = Array.Empty<ResourceContent>();
}

/// <summary>
/// リソースコンテンツ
/// </summary>
public class ResourceContent
{
    [JsonPropertyName("uri")]
    public required string Uri { get; set; }

    [JsonPropertyName("mimeType")]
    public required string MimeType { get; set; }

    [JsonPropertyName("text")]
    public required string Text { get; set; }
}
