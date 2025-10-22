using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Nodes;

using Microsoft.Extensions.Logging;

using RedmineCLI.Models;
using RedmineCLI.Models.Mcp;

namespace RedmineCLI.Services.Mcp;

/// <summary>
/// Model Context Protocol (MCP) サーバー実装
/// JSON-RPC 2.0プロトコルでRedmine操作を提供
/// </summary>
public class McpServer
{
    private readonly IRedmineService _redmineService;
    private readonly ILogger<McpServer> _logger;

    public McpServer(IRedmineService redmineService, ILogger<McpServer> logger)
    {
        _redmineService = redmineService;
        _logger = logger;
    }

    /// <summary>
    /// JSON-RPCリクエストを処理してレスポンスを返す
    /// </summary>
    [RequiresUnreferencedCode("JSON-RPC message handling requires dynamic type resolution")]
    public async Task<JsonRpcResponse> HandleRequestAsync(JsonRpcRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Handling MCP request: {Method}", request.Method);

            return request.Method switch
            {
                "initialize" => HandleInitialize(request),
                "tools/list" => HandleToolsList(request),
                "tools/call" => await HandleToolsCallAsync(request, cancellationToken),
                "resources/list" => HandleResourcesList(request),
                "resources/read" => await HandleResourcesReadAsync(request, cancellationToken),
                _ => CreateErrorResponse(request.Id, JsonRpcError.CreateMethodNotFoundError(request.Method))
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling MCP request: {Method}", request.Method);
            return CreateErrorResponse(request.Id, JsonRpcError.CreateInternalError(ex.Message));
        }
    }

    #region Initialize

    private JsonRpcResponse HandleInitialize(JsonRpcRequest request)
    {
        var result = new
        {
            protocolVersion = "2024-11-05",
            capabilities = new
            {
                tools = new { },
                resources = new { }
            },
            serverInfo = new
            {
                name = "redmine",
                version = "1.0.0"
            }
        };

        return new JsonRpcResponse
        {
            Id = request.Id,
            Result = result
        };
    }

    #endregion

    #region Tools

    private JsonRpcResponse HandleToolsList(JsonRpcRequest request)
    {
        var tools = new object[]
        {
            new
            {
                name = "get_issues",
                description = "Get a list of Redmine issues with optional filters",
                inputSchema = new
                {
                    type = "object",
                    properties = new
                    {
                        assignedTo = new { type = "string", description = "Filter by assigned user (use '@me' for current user)" },
                        status = new { type = "string", description = "Filter by status (e.g., 'open', 'closed', or specific status name)" },
                        project = new { type = "string", description = "Filter by project identifier" },
                        limit = new { type = "integer", description = "Maximum number of issues to return" }
                    }
                }
            },
            new
            {
                name = "get_issue",
                description = "Get details of a specific Redmine issue by ID",
                inputSchema = new
                {
                    type = "object",
                    properties = new
                    {
                        issueId = new { type = "integer", description = "Issue ID" }
                    },
                    required = new[] { "issueId" }
                }
            },
            new
            {
                name = "create_issue",
                description = "Create a new Redmine issue",
                inputSchema = new
                {
                    type = "object",
                    properties = new
                    {
                        project = new { type = "string", description = "Project identifier" },
                        subject = new { type = "string", description = "Issue subject/title" },
                        description = new { type = "string", description = "Issue description" },
                        priority = new { type = "string", description = "Priority name" },
                        assignedTo = new { type = "string", description = "Assigned user login name" }
                    },
                    required = new[] { "project", "subject" }
                }
            },
            new
            {
                name = "update_issue",
                description = "Update an existing Redmine issue",
                inputSchema = new
                {
                    type = "object",
                    properties = new
                    {
                        issueId = new { type = "integer", description = "Issue ID" },
                        status = new { type = "string", description = "New status" },
                        assignedTo = new { type = "string", description = "New assigned user" },
                        doneRatio = new { type = "integer", description = "Done ratio (0-100)" },
                        notes = new { type = "string", description = "Update notes" }
                    },
                    required = new[] { "issueId" }
                }
            },
            new
            {
                name = "add_comment",
                description = "Add a comment to a Redmine issue",
                inputSchema = new
                {
                    type = "object",
                    properties = new
                    {
                        issueId = new { type = "integer", description = "Issue ID" },
                        comment = new { type = "string", description = "Comment text" }
                    },
                    required = new[] { "issueId", "comment" }
                }
            },
            new
            {
                name = "get_projects",
                description = "Get a list of Redmine projects",
                inputSchema = new
                {
                    type = "object",
                    properties = new { }
                }
            },
            new
            {
                name = "get_users",
                description = "Get a list of Redmine users",
                inputSchema = new
                {
                    type = "object",
                    properties = new
                    {
                        limit = new { type = "integer", description = "Maximum number of users to return" }
                    }
                }
            },
            new
            {
                name = "get_statuses",
                description = "Get a list of issue statuses",
                inputSchema = new
                {
                    type = "object",
                    properties = new { }
                }
            },
            new
            {
                name = "search",
                description = "Search for issues by keyword",
                inputSchema = new
                {
                    type = "object",
                    properties = new
                    {
                        query = new { type = "string", description = "Search query" }
                    },
                    required = new[] { "query" }
                }
            }
        };

        return new JsonRpcResponse
        {
            Id = request.Id,
            Result = new { tools }
        };
    }

    [RequiresUnreferencedCode("Calls System.Text.Json.JsonSerializer.Serialize<TValue>(TValue, JsonSerializerOptions)")]
    private async Task<JsonRpcResponse> HandleToolsCallAsync(JsonRpcRequest request, CancellationToken cancellationToken)
    {
        // Parse params
        var paramsJson = JsonSerializer.Serialize(request.Params);
        var paramsNode = JsonNode.Parse(paramsJson);
        var toolName = paramsNode?["name"]?.GetValue<string>();
        var argumentsNode = paramsNode?["arguments"];

        if (string.IsNullOrEmpty(toolName))
        {
            return CreateErrorResponse(request.Id, JsonRpcError.CreateInvalidParamsError("Missing 'name' parameter"));
        }

        try
        {
            var result = toolName switch
            {
                "get_issues" => await ExecuteGetIssuesAsync(argumentsNode, cancellationToken),
                "get_issue" => await ExecuteGetIssueAsync(argumentsNode, cancellationToken),
                "create_issue" => await ExecuteCreateIssueAsync(argumentsNode, cancellationToken),
                "update_issue" => await ExecuteUpdateIssueAsync(argumentsNode, cancellationToken),
                "add_comment" => await ExecuteAddCommentAsync(argumentsNode, cancellationToken),
                "get_projects" => await ExecuteGetProjectsAsync(cancellationToken),
                "get_users" => await ExecuteGetUsersAsync(argumentsNode, cancellationToken),
                "get_statuses" => await ExecuteGetStatusesAsync(cancellationToken),
                "search" => await ExecuteSearchAsync(argumentsNode, cancellationToken),
                _ => throw new InvalidOperationException($"Unknown tool: {toolName}")
            };

            return new JsonRpcResponse
            {
                Id = request.Id,
                Result = new { content = new[] { new { type = "text", text = JsonSerializer.Serialize(result) } } }
            };
        }
        catch (InvalidOperationException ex) when (ex.Message.StartsWith("Unknown tool"))
        {
            return CreateErrorResponse(request.Id, JsonRpcError.CreateMethodNotFoundError(toolName));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing tool: {ToolName}", toolName);
            return CreateErrorResponse(request.Id, JsonRpcError.CreateInternalError(ex.Message));
        }
    }

    #endregion

    #region Resources

    private JsonRpcResponse HandleResourcesList(JsonRpcRequest request)
    {
        var resources = new[]
        {
            new
            {
                uri = "issue://{id}",
                name = "Redmine Issue",
                description = "Get details of a specific issue by ID",
                mimeType = "application/json"
            },
            new
            {
                uri = "issues://",
                name = "My Issues",
                description = "List of issues assigned to the current user",
                mimeType = "application/json"
            },
            new
            {
                uri = "project://{id}/issues",
                name = "Project Issues",
                description = "List of issues in a specific project",
                mimeType = "application/json"
            }
        };

        return new JsonRpcResponse
        {
            Id = request.Id,
            Result = new { resources }
        };
    }

    [RequiresUnreferencedCode("Calls System.Text.Json.JsonSerializer.Serialize<TValue>(TValue, JsonSerializerOptions)")]
    private async Task<JsonRpcResponse> HandleResourcesReadAsync(JsonRpcRequest request, CancellationToken cancellationToken)
    {
        // Parse params
        var paramsJson = JsonSerializer.Serialize(request.Params);
        var paramsNode = JsonNode.Parse(paramsJson);
        var uri = paramsNode?["uri"]?.GetValue<string>();

        if (string.IsNullOrEmpty(uri))
        {
            return CreateErrorResponse(request.Id, JsonRpcError.CreateInvalidParamsError("Missing 'uri' parameter"));
        }

        try
        {
            object? content;

            if (uri.StartsWith("issue://"))
            {
                var issueId = int.Parse(uri.Replace("issue://", ""));
                content = await _redmineService.GetIssueAsync(issueId, false, cancellationToken);
            }
            else if (uri == "issues://")
            {
                var filter = new IssueFilter { AssignedToId = "@me" };
                content = await _redmineService.GetIssuesAsync(filter, cancellationToken);
            }
            else if (uri.StartsWith("project://") && uri.Contains("/issues"))
            {
                var projectId = uri.Replace("project://", "").Replace("/issues", "");
                var filter = new IssueFilter { ProjectId = projectId };
                content = await _redmineService.GetIssuesAsync(filter, cancellationToken);
            }
            else
            {
                return CreateErrorResponse(request.Id, JsonRpcError.CreateInvalidParamsError($"Unknown resource URI: {uri}"));
            }

            return new JsonRpcResponse
            {
                Id = request.Id,
                Result = new
                {
                    contents = new[]
                    {
                        new
                        {
                            uri,
                            mimeType = "application/json",
                            text = JsonSerializer.Serialize(content)
                        }
                    }
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading resource: {Uri}", uri);
            return CreateErrorResponse(request.Id, JsonRpcError.CreateInternalError(ex.Message));
        }
    }

    #endregion

    #region Tool Implementations

    private async Task<object> ExecuteGetIssuesAsync(JsonNode? arguments, CancellationToken cancellationToken)
    {
        var filter = new IssueFilter();

        if (arguments != null)
        {
            filter.AssignedToId = arguments["assignedTo"]?.GetValue<string>();
            filter.StatusId = arguments["status"]?.GetValue<string>();
            filter.ProjectId = arguments["project"]?.GetValue<string>();

            if (arguments["limit"] != null)
            {
                filter.Limit = arguments["limit"]!.GetValue<int>();
            }
        }

        return await _redmineService.GetIssuesAsync(filter, cancellationToken);
    }

    private async Task<object> ExecuteGetIssueAsync(JsonNode? arguments, CancellationToken cancellationToken)
    {
        if (arguments?["issueId"] == null)
        {
            throw new ArgumentException("Missing required parameter: issueId");
        }

        var issueId = arguments["issueId"]!.GetValue<int>();
        bool includeJournals = arguments["includeJournals"]?.GetValue<bool>() ?? false;
        return await _redmineService.GetIssueAsync(issueId, includeJournals, cancellationToken);
    }

    private async Task<object> ExecuteCreateIssueAsync(JsonNode? arguments, CancellationToken cancellationToken)
    {
        if (arguments?["project"] == null || arguments["subject"] == null)
        {
            throw new ArgumentException("Missing required parameters: project, subject");
        }

        var projectIdentifier = arguments["project"]!.GetValue<string>()!;
        var subject = arguments["subject"]!.GetValue<string>()!;
        var description = arguments["description"]?.GetValue<string>();
        var assignedToLogin = arguments["assignedTo"]?.GetValue<string>();

        return await _redmineService.CreateIssueAsync(
            projectIdentifier,
            subject,
            description,
            assignedToLogin,
            cancellationToken);
    }

    private async Task<object> ExecuteUpdateIssueAsync(JsonNode? arguments, CancellationToken cancellationToken)
    {
        if (arguments?["issueId"] == null)
        {
            throw new ArgumentException("Missing required parameter: issueId");
        }

        var issueId = arguments["issueId"]!.GetValue<int>();
        var subject = arguments["subject"]?.GetValue<string>();
        var statusName = arguments["status"]?.GetValue<string>();
        var assignedToLogin = arguments["assignedTo"]?.GetValue<string>();
        var description = arguments["description"]?.GetValue<string>();
        int? doneRatio = arguments["doneRatio"]?.GetValue<int>();

        await _redmineService.UpdateIssueAsync(
            issueId,
            subject,
            statusName,
            assignedToLogin,
            description,
            doneRatio,
            cancellationToken);

        return new { success = true, issueId };
    }

    private async Task<object> ExecuteAddCommentAsync(JsonNode? arguments, CancellationToken cancellationToken)
    {
        if (arguments?["issueId"] == null || arguments["comment"] == null)
        {
            throw new ArgumentException("Missing required parameters: issueId, comment");
        }

        var issueId = arguments["issueId"]!.GetValue<int>();
        var comment = arguments["comment"]!.GetValue<string>()!;

        await _redmineService.AddCommentAsync(issueId, comment, cancellationToken);
        return new { success = true, issueId };
    }

    private async Task<object> ExecuteGetProjectsAsync(CancellationToken cancellationToken)
    {
        return await _redmineService.GetProjectsAsync(cancellationToken);
    }

    private async Task<object> ExecuteGetUsersAsync(JsonNode? arguments, CancellationToken cancellationToken)
    {
        int? limit = arguments?["limit"]?.GetValue<int>();
        return await _redmineService.GetUsersAsync(limit, cancellationToken);
    }

    private async Task<object> ExecuteGetStatusesAsync(CancellationToken cancellationToken)
    {
        return await _redmineService.GetIssueStatusesAsync(cancellationToken);
    }

    private async Task<object> ExecuteSearchAsync(JsonNode? arguments, CancellationToken cancellationToken)
    {
        if (arguments?["query"] == null)
        {
            throw new ArgumentException("Missing required parameter: query");
        }

        var query = arguments["query"]!.GetValue<string>()!;
        var assignedTo = arguments["assignedTo"]?.GetValue<string>();
        var status = arguments["status"]?.GetValue<string>();
        var project = arguments["project"]?.GetValue<string>();
        int? limit = arguments["limit"]?.GetValue<int>();

        return await _redmineService.SearchIssuesAsync(
            query,
            assignedTo,
            status,
            project,
            limit,
            null,
            null,
            cancellationToken);
    }

    #endregion

    #region Helper Methods

    private static JsonRpcResponse CreateErrorResponse(object? id, JsonRpcError error)
    {
        return new JsonRpcResponse
        {
            Id = id,
            Error = error
        };
    }

    #endregion
}
