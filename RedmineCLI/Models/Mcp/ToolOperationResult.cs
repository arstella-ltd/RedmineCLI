using System.Text.Json.Serialization;

namespace RedmineCLI.Models.Mcp;

/// <summary>
/// Tool操作の結果
/// Native AOT互換性のため匿名型の代わりに使用
/// </summary>
public class ToolOperationResult
{
    /// <summary>
    /// 操作が成功したかどうか
    /// </summary>
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    /// <summary>
    /// Issue ID
    /// </summary>
    [JsonPropertyName("issueId")]
    public int IssueId { get; set; }
}
