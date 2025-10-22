using System.CommandLine;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

using Microsoft.Extensions.Logging;

using RedmineCLI.Models.Mcp;
using RedmineCLI.Services;
using RedmineCLI.Services.Mcp;

namespace RedmineCLI.Commands;

/// <summary>
/// MCPサーバーコマンド
/// </summary>
public class McpCommand
{
    private readonly IRedmineService _redmineService;
    private readonly ILogger<McpCommand> _logger;
    private readonly ILogger<McpServer> _mcpServerLogger;

    public McpCommand(IRedmineService redmineService, ILogger<McpCommand> logger, ILogger<McpServer> mcpServerLogger)
    {
        _redmineService = redmineService;
        _logger = logger;
        _mcpServerLogger = mcpServerLogger;
    }

    /// <summary>
    /// MCPコマンドを作成
    /// </summary>
    public static Command Create(IRedmineService redmineService, ILogger<McpCommand> logger, ILogger<McpServer> mcpServerLogger)
    {
        var mcpCommand = new McpCommand(redmineService, logger, mcpServerLogger);

        var command = new Command("mcp", "Start MCP (Model Context Protocol) server");

        var debugOption = new Option<bool>("--debug") { Description = "Enable debug logging" };
        command.Add(debugOption);

#pragma warning disable IL2026 // MCP server requires dynamic JSON serialization
        command.SetAction(async (parseResult) =>
        {
            var debug = parseResult.GetValue(debugOption);
            Environment.ExitCode = await mcpCommand.StartServerAsync(debug);
        });
#pragma warning restore IL2026

        return command;
    }

    /// <summary>
    /// MCPサーバーを起動
    /// </summary>
    [RequiresUnreferencedCode("JSON-RPC message handling requires dynamic type resolution")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Trimming", "IL2026", Justification = "MCP server requires dynamic JSON serialization")]
    private async Task<int> StartServerAsync(bool debug)
    {
        try
        {
            var server = new McpServer(_redmineService, _mcpServerLogger);

            if (debug)
            {
                await Console.Error.WriteLineAsync("MCP server starting (debug mode)...");
            }

            // stdio通信ループ
            while (true)
            {
                var line = await Console.In.ReadLineAsync();
                if (line == null)
                {
                    // EOF reached
                    break;
                }

                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                if (debug)
                {
                    await Console.Error.WriteLineAsync($"Received: {line}");
                }

                try
                {
                    // JSON-RPCリクエストをパース
                    var request = JsonSerializer.Deserialize(line, McpJsonContext.Default.JsonRpcRequest);
                    if (request == null)
                    {
                        var errorResponse = new JsonRpcResponse
                        {
                            Id = null,
                            Error = JsonRpcError.CreateParseError("Failed to parse request")
                        };
                        var errorJson = JsonSerializer.Serialize(errorResponse, McpJsonContext.Default.JsonRpcResponse);
                        await Console.Out.WriteLineAsync(errorJson);
                        continue;
                    }

                    // リクエストを処理
                    var response = await server.HandleRequestAsync(request);

                    // レスポンスを返す
                    var responseJson = JsonSerializer.Serialize(response, McpJsonContext.Default.JsonRpcResponse);
                    await Console.Out.WriteLineAsync(responseJson);

                    if (debug)
                    {
                        await Console.Error.WriteLineAsync($"Sent: {responseJson}");
                    }
                }
                catch (JsonException ex)
                {
                    if (debug)
                    {
                        await Console.Error.WriteLineAsync($"JSON parsing error: {ex.Message}");
                    }

                    var errorResponse = new JsonRpcResponse
                    {
                        Id = null,
                        Error = JsonRpcError.CreateParseError(ex.Message)
                    };
                    var errorJson = JsonSerializer.Serialize(errorResponse, McpJsonContext.Default.JsonRpcResponse);
                    await Console.Out.WriteLineAsync(errorJson);
                }
                catch (Exception ex)
                {
                    if (debug)
                    {
                        await Console.Error.WriteLineAsync($"Error: {ex.Message}");
                        await Console.Error.WriteLineAsync(ex.StackTrace);
                    }

                    var errorResponse = new JsonRpcResponse
                    {
                        Id = null,
                        Error = JsonRpcError.CreateInternalError(ex.Message)
                    };
                    var errorJson = JsonSerializer.Serialize(errorResponse, McpJsonContext.Default.JsonRpcResponse);
                    await Console.Out.WriteLineAsync(errorJson);
                }
            }

            return 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting MCP server");
            await Console.Error.WriteLineAsync($"Error: {ex.Message}");
            return 1;
        }
    }
}
