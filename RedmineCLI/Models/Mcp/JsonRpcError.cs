using System.Text.Json.Serialization;

namespace RedmineCLI.Models.Mcp;

/// <summary>
/// JSON-RPC 2.0 エラー情報
/// </summary>
public class JsonRpcError
{
    // Standard JSON-RPC 2.0 error codes
    public const int ParseError = -32700;
    public const int InvalidRequest = -32600;
    public const int MethodNotFound = -32601;
    public const int InvalidParams = -32602;
    public const int InternalError = -32603;

    /// <summary>
    /// エラーコード
    /// </summary>
    [JsonPropertyName("code")]
    public int Code { get; set; }

    /// <summary>
    /// エラーメッセージ
    /// </summary>
    [JsonPropertyName("message")]
    public required string Message { get; set; }

    /// <summary>
    /// 追加のエラー情報（オプション）
    /// </summary>
    [JsonPropertyName("data")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public object? Data { get; set; }

    /// <summary>
    /// Parse errorを作成
    /// </summary>
    public static JsonRpcError CreateParseError(string details)
    {
        return new JsonRpcError
        {
            Code = ParseError,
            Message = "Parse error",
            Data = details
        };
    }

    /// <summary>
    /// Invalid Request errorを作成
    /// </summary>
    public static JsonRpcError CreateInvalidRequestError(string details)
    {
        return new JsonRpcError
        {
            Code = InvalidRequest,
            Message = "Invalid Request",
            Data = details
        };
    }

    /// <summary>
    /// Method not found errorを作成
    /// </summary>
    public static JsonRpcError CreateMethodNotFoundError(string methodName)
    {
        return new JsonRpcError
        {
            Code = MethodNotFound,
            Message = "Method not found",
            Data = new { method = methodName }
        };
    }

    /// <summary>
    /// Invalid params errorを作成
    /// </summary>
    public static JsonRpcError CreateInvalidParamsError(string details)
    {
        return new JsonRpcError
        {
            Code = InvalidParams,
            Message = "Invalid params",
            Data = details
        };
    }

    /// <summary>
    /// Internal errorを作成
    /// </summary>
    public static JsonRpcError CreateInternalError(string details)
    {
        return new JsonRpcError
        {
            Code = InternalError,
            Message = "Internal error",
            Data = details
        };
    }
}
