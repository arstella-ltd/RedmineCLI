using System.Text.Json;
using System.Text.Json.Serialization;

namespace RedmineCLI.Models.Mcp;

/// <summary>
/// JSON-RPC 2.0 リクエストメッセージ
/// </summary>
public class JsonRpcRequest
{
    /// <summary>
    /// JSON-RPCプロトコルバージョン（常に "2.0"）
    /// </summary>
    [JsonPropertyName("jsonrpc")]
    public string JsonRpc { get; set; } = "2.0";

    /// <summary>
    /// リクエストID（レスポンスとの対応付けに使用）
    /// </summary>
    [JsonPropertyName("id")]
    public object? Id { get; set; }

    /// <summary>
    /// 呼び出すメソッド名
    /// </summary>
    [JsonPropertyName("method")]
    public required string Method { get; set; }

    /// <summary>
    /// メソッドパラメータ（オプション）
    /// </summary>
    [JsonPropertyName("params")]
    public object? Params { get; set; }
}
