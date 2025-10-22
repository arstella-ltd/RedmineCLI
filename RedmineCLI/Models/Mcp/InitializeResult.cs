using System.Text.Json.Serialization;

namespace RedmineCLI.Models.Mcp;

/// <summary>
/// MCP initialize レスポンス
/// </summary>
public class InitializeResult
{
    [JsonPropertyName("protocolVersion")]
    public required string ProtocolVersion { get; set; }

    [JsonPropertyName("capabilities")]
    public required Capabilities Capabilities { get; set; }

    [JsonPropertyName("serverInfo")]
    public required ServerInfo ServerInfo { get; set; }
}

/// <summary>
/// MCPサーバーの能力
/// </summary>
public class Capabilities
{
    [JsonPropertyName("tools")]
    public ToolsCapability Tools { get; set; } = new();

    [JsonPropertyName("resources")]
    public ResourcesCapability Resources { get; set; } = new();
}

/// <summary>
/// Tools能力（空のオブジェクト）
/// </summary>
public class ToolsCapability
{
}

/// <summary>
/// Resources能力（空のオブジェクト）
/// </summary>
public class ResourcesCapability
{
}

/// <summary>
/// MCPサーバー情報
/// </summary>
public class ServerInfo
{
    [JsonPropertyName("name")]
    public required string Name { get; set; }

    [JsonPropertyName("version")]
    public required string Version { get; set; }
}
