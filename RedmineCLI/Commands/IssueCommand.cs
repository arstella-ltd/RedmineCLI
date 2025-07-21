using System.CommandLine;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;
using RedmineCLI.ApiClient;
using RedmineCLI.Formatters;
using RedmineCLI.Models;
using RedmineCLI.Services;
using Spectre.Console;

namespace RedmineCLI.Commands;

public class IssueCommand
{
    private readonly IRedmineApiClient _apiClient;
    private readonly IConfigService _configService;
    private readonly ITableFormatter _tableFormatter;
    private readonly IJsonFormatter _jsonFormatter;
    private readonly ILogger<IssueCommand> _logger;

    public IssueCommand(
        IRedmineApiClient apiClient,
        IConfigService configService,
        ITableFormatter tableFormatter,
        IJsonFormatter jsonFormatter,
        ILogger<IssueCommand> logger)
    {
        _apiClient = apiClient;
        _configService = configService;
        _tableFormatter = tableFormatter;
        _jsonFormatter = jsonFormatter;
        _logger = logger;
    }

    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(IssueCommand))]
    public static Command Create(
        IRedmineApiClient apiClient,
        IConfigService configService,
        ITableFormatter tableFormatter,
        IJsonFormatter jsonFormatter,
        ILogger<IssueCommand> logger)
    {
        var command = new Command("issue", "Manage Redmine issues");
        var issueCommand = new IssueCommand(apiClient, configService, tableFormatter, jsonFormatter, logger);

        var listCommand = new Command("list", "List issues with optional filters");
        
        var assigneeOption = new Option<string?>("--assignee") { Description = "Filter by assignee (username, ID, or @me)" };
        assigneeOption.Aliases.Add("-a");
        var statusOption = new Option<string?>("--status") { Description = "Filter by status (open, closed, all, or status ID)" };
        statusOption.Aliases.Add("-s");
        var projectOption = new Option<string?>("--project") { Description = "Filter by project (identifier or ID)" };
        projectOption.Aliases.Add("-p");
        var limitOption = new Option<int?>("--limit") { Description = "Limit number of results (default: 30)" };
        limitOption.Aliases.Add("-L");
        var offsetOption = new Option<int?>("--offset") { Description = "Offset for pagination" };
        var jsonOption = new Option<bool>("--json") { Description = "Output in JSON format" };

        listCommand.Add(assigneeOption);
        listCommand.Add(statusOption);
        listCommand.Add(projectOption);
        listCommand.Add(limitOption);
        listCommand.Add(offsetOption);
        listCommand.Add(jsonOption);

        listCommand.SetAction(async (parseResult) =>
        {
            var assignee = parseResult.GetValue(assigneeOption);
            var status = parseResult.GetValue(statusOption);
            var project = parseResult.GetValue(projectOption);
            var limit = parseResult.GetValue(limitOption);
            var offset = parseResult.GetValue(offsetOption);
            var json = parseResult.GetValue(jsonOption);
            
            Environment.ExitCode = await issueCommand.ListAsync(assignee, status, project, limit, offset, json, CancellationToken.None);
        });

        command.Add(listCommand);
        return command;
    }

    public async Task<int> ListAsync(
        string? assignee,
        string? status,
        string? project,
        int? limit,
        int? offset,
        bool json,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Listing issues with filters - Assignee: {Assignee}, Status: {Status}, Project: {Project}",
                assignee, status, project);

            // Handle @me special value
            if (assignee == "@me")
            {
                var currentUser = await _apiClient.GetCurrentUserAsync(cancellationToken);
                assignee = currentUser.Id.ToString();
            }

            // Handle status special values
            string? statusFilter = status;
            if (status == "all")
            {
                statusFilter = null; // No status filter means all statuses
            }

            var filter = new IssueFilter
            {
                AssignedToId = assignee,
                StatusId = statusFilter,
                ProjectId = project,
                Limit = limit ?? 30, // Default limit to 30
                Offset = offset
            };

            // If no filters are specified, default to open issues
            if (string.IsNullOrEmpty(assignee) && string.IsNullOrEmpty(status) && string.IsNullOrEmpty(project))
            {
                filter.StatusId = "open";
            }

            var issues = await _apiClient.GetIssuesAsync(filter, cancellationToken);

            if (json)
            {
                _jsonFormatter.FormatIssues(issues);
            }
            else
            {
                _tableFormatter.FormatIssues(issues);
            }

            return 0;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "API request failed");
            AnsiConsole.MarkupLine("[red]Error: API request failed[/]");
            return 1;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to list issues");
            AnsiConsole.MarkupLine($"[red]Error: {ex.Message}[/]");
            return 1;
        }
    }
}