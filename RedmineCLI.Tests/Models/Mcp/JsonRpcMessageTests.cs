using System.Text.Json;

using FluentAssertions;

using RedmineCLI.Models.Mcp;

using Xunit;

namespace RedmineCLI.Tests.Models.Mcp;

public class JsonRpcMessageTests
{
    private readonly JsonSerializerOptions _jsonOptions;

    public JsonRpcMessageTests()
    {
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };
    }

    #region JsonRpcRequest Tests

    [Fact]
    public void JsonRpcRequest_Should_SerializeCorrectly_When_ValidRequest()
    {
        // Arrange
        var request = new JsonRpcRequest
        {
            JsonRpc = "2.0",
            Id = "test-id-1",
            Method = "initialize",
            Params = new { protocolVersion = "2024-11-05" }
        };

        // Act
        var json = JsonSerializer.Serialize(request, _jsonOptions);
        var deserialized = JsonSerializer.Deserialize<JsonRpcRequest>(json, _jsonOptions);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized!.JsonRpc.Should().Be("2.0");
        deserialized.Id.Should().NotBeNull();
        deserialized.Id.ToString().Should().Be("test-id-1");
        deserialized.Method.Should().Be("initialize");
    }

    [Fact]
    public void JsonRpcRequest_Should_DeserializeCorrectly_When_ValidJsonString()
    {
        // Arrange
        var json = """{"jsonrpc":"2.0","id":"req-1","method":"tools/list","params":{}}""";

        // Act
        var request = JsonSerializer.Deserialize<JsonRpcRequest>(json, _jsonOptions);

        // Assert
        request.Should().NotBeNull();
        request!.JsonRpc.Should().Be("2.0");
        request.Id.Should().NotBeNull();
        request.Id.ToString().Should().Be("req-1");
        request.Method.Should().Be("tools/list");
    }

    [Fact]
    public void JsonRpcRequest_Should_HandleIntegerId_When_Deserializing()
    {
        // Arrange
        var json = """{"jsonrpc":"2.0","id":123,"method":"test"}""";

        // Act
        var request = JsonSerializer.Deserialize<JsonRpcRequest>(json, _jsonOptions);

        // Assert
        request.Should().NotBeNull();
        request!.Id.Should().NotBeNull();
    }

    #endregion

    #region JsonRpcResponse Tests

    [Fact]
    public void JsonRpcResponse_Should_SerializeCorrectly_When_ValidResult()
    {
        // Arrange
        var response = new JsonRpcResponse
        {
            JsonRpc = "2.0",
            Id = "test-id-1",
            Result = new { serverName = "redmine", version = "1.0.0" }
        };

        // Act
        var json = JsonSerializer.Serialize(response, _jsonOptions);
        var deserialized = JsonSerializer.Deserialize<JsonRpcResponse>(json, _jsonOptions);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized!.JsonRpc.Should().Be("2.0");
        deserialized.Id.Should().NotBeNull();
        deserialized.Id.ToString().Should().Be("test-id-1");
        deserialized.Result.Should().NotBeNull();
        deserialized.Error.Should().BeNull();
    }

    [Fact]
    public void JsonRpcResponse_Should_SerializeCorrectly_When_Error()
    {
        // Arrange
        var response = new JsonRpcResponse
        {
            JsonRpc = "2.0",
            Id = "test-id-1",
            Error = new JsonRpcError
            {
                Code = -32600,
                Message = "Invalid Request"
            }
        };

        // Act
        var json = JsonSerializer.Serialize(response, _jsonOptions);
        var deserialized = JsonSerializer.Deserialize<JsonRpcResponse>(json, _jsonOptions);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized!.JsonRpc.Should().Be("2.0");
        deserialized.Id.Should().NotBeNull();
        deserialized.Id.ToString().Should().Be("test-id-1");
        deserialized.Result.Should().BeNull();
        deserialized.Error.Should().NotBeNull();
        deserialized.Error!.Code.Should().Be(-32600);
        deserialized.Error.Message.Should().Be("Invalid Request");
    }

    [Fact]
    public void JsonRpcResponse_Should_DeserializeCorrectly_When_ValidJsonString()
    {
        // Arrange
        var json = """{"jsonrpc":"2.0","id":"res-1","result":{"status":"ok"}}""";

        // Act
        var response = JsonSerializer.Deserialize<JsonRpcResponse>(json, _jsonOptions);

        // Assert
        response.Should().NotBeNull();
        response!.JsonRpc.Should().Be("2.0");
        response.Id.Should().NotBeNull();
        response.Id.ToString().Should().Be("res-1");
        response.Result.Should().NotBeNull();
    }

    #endregion

    #region JsonRpcError Tests

    [Fact]
    public void JsonRpcError_Should_HaveCorrectErrorCodes()
    {
        // Assert
        JsonRpcError.ParseError.Should().Be(-32700);
        JsonRpcError.InvalidRequest.Should().Be(-32600);
        JsonRpcError.MethodNotFound.Should().Be(-32601);
        JsonRpcError.InvalidParams.Should().Be(-32602);
        JsonRpcError.InternalError.Should().Be(-32603);
    }

    [Fact]
    public void JsonRpcError_Should_SerializeWithData_When_DataProvided()
    {
        // Arrange
        var error = new JsonRpcError
        {
            Code = -32602,
            Message = "Invalid params",
            Data = new { paramName = "projectId", reason = "missing" }
        };

        // Act
        var json = JsonSerializer.Serialize(error, _jsonOptions);
        var deserialized = JsonSerializer.Deserialize<JsonRpcError>(json, _jsonOptions);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized!.Code.Should().Be(-32602);
        deserialized.Message.Should().Be("Invalid params");
        deserialized.Data.Should().NotBeNull();
    }

    [Fact]
    public void JsonRpcError_Should_CreateParseError()
    {
        // Act
        var error = JsonRpcError.CreateParseError("Invalid JSON");

        // Assert
        error.Code.Should().Be(-32700);
        error.Message.Should().Be("Parse error");
        error.Data.Should().NotBeNull();
    }

    [Fact]
    public void JsonRpcError_Should_CreateInvalidRequestError()
    {
        // Act
        var error = JsonRpcError.CreateInvalidRequestError("Missing method field");

        // Assert
        error.Code.Should().Be(-32600);
        error.Message.Should().Be("Invalid Request");
        error.Data.Should().NotBeNull();
    }

    [Fact]
    public void JsonRpcError_Should_CreateMethodNotFoundError()
    {
        // Act
        var error = JsonRpcError.CreateMethodNotFoundError("unknown_method");

        // Assert
        error.Code.Should().Be(-32601);
        error.Message.Should().Be("Method not found");
        error.Data.Should().NotBeNull();
    }

    [Fact]
    public void JsonRpcError_Should_CreateInvalidParamsError()
    {
        // Act
        var error = JsonRpcError.CreateInvalidParamsError("Missing required parameter");

        // Assert
        error.Code.Should().Be(-32602);
        error.Message.Should().Be("Invalid params");
        error.Data.Should().NotBeNull();
    }

    [Fact]
    public void JsonRpcError_Should_CreateInternalError()
    {
        // Act
        var error = JsonRpcError.CreateInternalError("Unexpected exception");

        // Assert
        error.Code.Should().Be(-32603);
        error.Message.Should().Be("Internal error");
        error.Data.Should().NotBeNull();
    }

    #endregion
}
