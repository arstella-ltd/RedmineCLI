using System.Text.Json;
using System.Text.Json.Serialization;

namespace RedmineCLI.Models.Mcp;

/// <summary>
/// MCP用のJSON Source Generator Context
/// Native AOT互換性のため
/// </summary>
[JsonSourceGenerationOptions(
    WriteIndented = false,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(JsonRpcRequest))]
[JsonSerializable(typeof(JsonRpcResponse))]
[JsonSerializable(typeof(JsonRpcError))]
[JsonSerializable(typeof(InitializeResult))]
[JsonSerializable(typeof(Capabilities))]
[JsonSerializable(typeof(ToolsCapability))]
[JsonSerializable(typeof(ResourcesCapability))]
[JsonSerializable(typeof(ServerInfo))]
[JsonSerializable(typeof(Tool))]
[JsonSerializable(typeof(Tool[]))]
[JsonSerializable(typeof(InputSchema))]
[JsonSerializable(typeof(SchemaProperty))]
[JsonSerializable(typeof(Dictionary<string, SchemaProperty>))]
[JsonSerializable(typeof(ToolsListResult))]
[JsonSerializable(typeof(Resource))]
[JsonSerializable(typeof(Resource[]))]
[JsonSerializable(typeof(ResourcesListResult))]
[JsonSerializable(typeof(ToolCallResult))]
[JsonSerializable(typeof(TextContent))]
[JsonSerializable(typeof(TextContent[]))]
[JsonSerializable(typeof(ResourceReadResult))]
[JsonSerializable(typeof(ResourceContent))]
[JsonSerializable(typeof(ResourceContent[]))]
[JsonSerializable(typeof(string[]))]
[JsonSerializable(typeof(JsonElement))]
[JsonSerializable(typeof(object))]
[JsonSerializable(typeof(string))]
[JsonSerializable(typeof(int))]
[JsonSerializable(typeof(bool))]
[JsonSerializable(typeof(ToolOperationResult))]
public partial class McpJsonContext : JsonSerializerContext
{
}
