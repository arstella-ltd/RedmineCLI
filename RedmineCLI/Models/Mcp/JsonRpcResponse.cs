using System.Text.Json.Serialization;

namespace RedmineCLI.Models.Mcp;

/// <summary>
/// JSON-RPC 2.0 レスポンスメッセージ
/// </summary>
public class JsonRpcResponse
{
    /// <summary>
    /// JSON-RPCプロトコルバージョン（常に "2.0"）
    /// </summary>
    [JsonPropertyName("jsonrpc")]
    public string JsonRpc { get; set; } = "2.0";

    /// <summary>
    /// リクエストID（リクエストと同じIDを返す）
    /// </summary>
    [JsonPropertyName("id")]
    public object? Id { get; set; }

    /// <summary>
    /// 成功時の結果（Errorと排他的）
    /// </summary>
    [JsonPropertyName("result")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public object? Result { get; set; }

    /// <summary>
    /// エラー時のエラー情報（Resultと排他的）
    /// </summary>
    [JsonPropertyName("error")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public JsonRpcError? Error { get; set; }
}
