using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization.Metadata;

using Microsoft.Extensions.Logging;

using RedmineCLI.Exceptions;
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
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        TypeInfoResolver = JsonTypeInfoResolver.Combine(
            McpJsonContext.Default,
            new DefaultJsonTypeInfoResolver())
    };

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
        var result = new InitializeResult
        {
            ProtocolVersion = "2024-11-05",
            Capabilities = new Capabilities
            {
                Tools = new ToolsCapability(),
                Resources = new ResourcesCapability()
            },
            ServerInfo = new ServerInfo
            {
                Name = "redmine",
                Version = "1.0.0"
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
        var tools = new Tool[]
        {
            new()
            {
                Name = "get_issues",
                Description = "Get a list of Redmine issues with optional filters",
                InputSchema = new InputSchema
                {
                    Type = "object",
                    Properties = new Dictionary<string, SchemaProperty>
                    {
                        ["assignedTo"] = new() { Type = "string", Description = "Filter by assigned user (use '@me' for current user)" },
                        ["status"] = new() { Type = "string", Description = "Filter by status (e.g., 'open', 'closed', or specific status name)" },
                        ["project"] = new() { Type = "string", Description = "Filter by project name or identifier" },
                        ["limit"] = new() { Type = "integer", Description = "Maximum number of issues to return" }
                    }
                }
            },
            new()
            {
                Name = "get_issue",
                Description = "Get details of a specific Redmine issue by ID",
                InputSchema = new InputSchema
                {
                    Type = "object",
                    Properties = new Dictionary<string, SchemaProperty>
                    {
                        ["issueId"] = new() { Type = "integer", Description = "Issue ID" },
                        ["includeJournals"] = new() { Type = "boolean", Description = "Include comments/journals (default: false)" }
                    },
                    Required = new[] { "issueId" }
                }
            },
            new()
            {
                Name = "create_issue",
                Description = "Create a new Redmine issue",
                InputSchema = new InputSchema
                {
                    Type = "object",
                    Properties = new Dictionary<string, SchemaProperty>
                    {
                        ["project"] = new() { Type = "string", Description = "Project identifier" },
                        ["subject"] = new() { Type = "string", Description = "Issue subject/title" },
                        ["description"] = new() { Type = "string", Description = "Issue description" },
                        ["assignedTo"] = new() { Type = "string", Description = "Assigned user login name" }
                    },
                    Required = new[] { "project", "subject" }
                }
            },
            new()
            {
                Name = "update_issue",
                Description = "Update an existing Redmine issue",
                InputSchema = new InputSchema
                {
                    Type = "object",
                    Properties = new Dictionary<string, SchemaProperty>
                    {
                        ["issueId"] = new() { Type = "integer", Description = "Issue ID" },
                        ["subject"] = new() { Type = "string", Description = "New subject/title" },
                        ["description"] = new() { Type = "string", Description = "New description" },
                        ["status"] = new() { Type = "string", Description = "New status name" },
                        ["assignedTo"] = new() { Type = "string", Description = "New assigned user login name" },
                        ["doneRatio"] = new() { Type = "integer", Description = "Done ratio (0-100)" }
                    },
                    Required = new[] { "issueId" }
                }
            },
            new()
            {
                Name = "add_comment",
                Description = "Add a comment to a Redmine issue",
                InputSchema = new InputSchema
                {
                    Type = "object",
                    Properties = new Dictionary<string, SchemaProperty>
                    {
                        ["issueId"] = new() { Type = "integer", Description = "Issue ID" },
                        ["comment"] = new() { Type = "string", Description = "Comment text" }
                    },
                    Required = new[] { "issueId", "comment" }
                }
            },
            new()
            {
                Name = "get_projects",
                Description = "Get a list of Redmine projects",
                InputSchema = new InputSchema
                {
                    Type = "object",
                    Properties = new Dictionary<string, SchemaProperty>()
                }
            },
            new()
            {
                Name = "get_users",
                Description = "Get a list of Redmine users",
                InputSchema = new InputSchema
                {
                    Type = "object",
                    Properties = new Dictionary<string, SchemaProperty>
                    {
                        ["limit"] = new() { Type = "integer", Description = "Maximum number of users to return" }
                    }
                }
            },
            new()
            {
                Name = "get_statuses",
                Description = "Get a list of issue statuses",
                InputSchema = new InputSchema
                {
                    Type = "object",
                    Properties = new Dictionary<string, SchemaProperty>()
                }
            },
            new()
            {
                Name = "search",
                Description = "Search for issues by keyword",
                InputSchema = new InputSchema
                {
                    Type = "object",
                    Properties = new Dictionary<string, SchemaProperty>
                    {
                        ["query"] = new() { Type = "string", Description = "Search query" },
                        ["assignedTo"] = new() { Type = "string", Description = "Filter by assigned user" },
                        ["status"] = new() { Type = "string", Description = "Filter by status" },
                        ["project"] = new() { Type = "string", Description = "Filter by project name or identifier" },
                        ["limit"] = new() { Type = "integer", Description = "Maximum number of results to return" }
                    },
                    Required = new[] { "query" }
                }
            }
        };

        return new JsonRpcResponse
        {
            Id = request.Id,
            Result = new ToolsListResult { Tools = tools }
        };
    }

    [RequiresUnreferencedCode("Calls System.Text.Json.JsonSerializer.Serialize<TValue>(TValue, JsonSerializerOptions)")]
    private async Task<JsonRpcResponse> HandleToolsCallAsync(JsonRpcRequest request, CancellationToken cancellationToken)
    {
        // Parse params
        var paramsJson = JsonSerializer.Serialize(request.Params, JsonOptions);
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
                Result = new ToolCallResult
                {
                    Content = new[]
                    {
                        new TextContent
                        {
                            Type = "text",
                            Text = JsonSerializer.Serialize(result, JsonOptions)
                        }
                    }
                }
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
        var resources = new Resource[]
        {
            new()
            {
                Uri = "issue://{id}",
                Name = "Redmine Issue",
                Description = "Get details of a specific issue by ID",
                MimeType = "application/json"
            },
            new()
            {
                Uri = "issues://",
                Name = "My Issues",
                Description = "List of issues assigned to the current user",
                MimeType = "application/json"
            },
            new()
            {
                Uri = "project://{id}/issues",
                Name = "Project Issues",
                Description = "List of issues in a specific project",
                MimeType = "application/json"
            }
        };

        return new JsonRpcResponse
        {
            Id = request.Id,
            Result = new ResourcesListResult { Resources = resources }
        };
    }

    [RequiresUnreferencedCode("Calls System.Text.Json.JsonSerializer.Serialize<TValue>(TValue, JsonSerializerOptions)")]
    private async Task<JsonRpcResponse> HandleResourcesReadAsync(JsonRpcRequest request, CancellationToken cancellationToken)
    {
        // Parse params
        var paramsJson = JsonSerializer.Serialize(request.Params, JsonOptions);
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
                Result = new ResourceReadResult
                {
                    Contents = new[]
                    {
                        new ResourceContent
                        {
                            Uri = uri,
                            MimeType = "application/json",
                            Text = JsonSerializer.Serialize(content, JsonOptions)
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

            // Resolve project name to identifier
            var projectParam = arguments["project"]?.GetValue<string>();
            if (!string.IsNullOrEmpty(projectParam))
            {
                filter.ProjectId = await ResolveProjectAsync(projectParam, cancellationToken);
            }

            if (arguments["limit"] != null)
            {
                filter.Limit = arguments["limit"]!.GetValue<int>();
            }
        }

        return await _redmineService.GetIssuesAsync(filter, cancellationToken);
    }

    /// <summary>
    /// Resolves a project name or identifier to a project identifier
    /// </summary>
    private async Task<string?> ResolveProjectAsync(string? project, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(project))
            return null;

        // 数値の場合はそのままIDとして返す
        if (int.TryParse(project, out _))
        {
            return project;
        }

        // 文字列の場合はプロジェクト名として扱い、プロジェクト識別子を検索する
        try
        {
            var projects = await _redmineService.GetProjectsAsync(cancellationToken);
            var matchedProject = projects.FirstOrDefault(p =>
                p.Name.Equals(project, StringComparison.OrdinalIgnoreCase) ||
                p.Identifier?.Equals(project, StringComparison.OrdinalIgnoreCase) == true);

            if (matchedProject != null)
            {
                _logger.LogDebug("Resolved project '{Project}' to identifier '{Identifier}'", project, matchedProject.Identifier);
                // プロジェクトの場合は識別子を返す（IDではなく）
                return matchedProject.Identifier ?? matchedProject.Id.ToString();
            }

            // プロジェクトが見つからない場合はエラーをスロー
            _logger.LogError("Could not find project with name '{Project}'", project);
            throw new ValidationException($"Project '{project}' not found.");
        }
        catch (ValidationException)
        {
            // ValidationExceptionはそのまま再スロー
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to resolve project '{Project}'", project);
            throw new ValidationException($"Failed to resolve project '{project}': {ex.Message}", ex);
        }
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
