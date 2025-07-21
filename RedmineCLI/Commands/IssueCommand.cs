using System.CommandLine;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
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
        var webOption = new Option<bool>("--web") { Description = "Open in web browser" };
        webOption.Aliases.Add("-w");

        listCommand.Add(assigneeOption);
        listCommand.Add(statusOption);
        listCommand.Add(projectOption);
        listCommand.Add(limitOption);
        listCommand.Add(offsetOption);
        listCommand.Add(jsonOption);
        listCommand.Add(webOption);

        listCommand.SetAction(async (parseResult) =>
        {
            var assignee = parseResult.GetValue(assigneeOption);
            var status = parseResult.GetValue(statusOption);
            var project = parseResult.GetValue(projectOption);
            var limit = parseResult.GetValue(limitOption);
            var offset = parseResult.GetValue(offsetOption);
            var json = parseResult.GetValue(jsonOption);
            var web = parseResult.GetValue(webOption);
            
            Environment.ExitCode = await issueCommand.ListAsync(assignee, status, project, limit, offset, json, web, CancellationToken.None);
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
        bool web,
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

            // Handle --web option
            if (web)
            {
                var activeProfile = await _configService.GetActiveProfileAsync();
                if (activeProfile?.Url == null)
                {
                    AnsiConsole.MarkupLine("[red]Error: No active profile found. Please login first.[/]");
                    return 1;
                }

                var url = BuildIssuesUrl(activeProfile.Url, filter);
                OpenInBrowser(url);
                AnsiConsole.MarkupLine($"[green]Opening issues in browser: {url}[/]");
                return 0;
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

    private static string BuildIssuesUrl(string baseUrl, IssueFilter filter)
    {
        var url = $"{baseUrl.TrimEnd('/')}/issues";
        var queryParams = new List<string>();

        if (!string.IsNullOrEmpty(filter.AssignedToId))
        {
            if (filter.AssignedToId == "me" || int.TryParse(filter.AssignedToId, out _))
            {
                queryParams.Add($"assigned_to_id={filter.AssignedToId}");
            }
            else
            {
                queryParams.Add($"assigned_to_id={Uri.EscapeDataString(filter.AssignedToId)}");
            }
        }

        if (!string.IsNullOrEmpty(filter.StatusId))
        {
            if (filter.StatusId == "open")
            {
                queryParams.Add("status_id=o"); // Redmine's open status parameter
            }
            else if (filter.StatusId == "closed")
            {
                queryParams.Add("status_id=c"); // Redmine's closed status parameter
            }
            else
            {
                queryParams.Add($"status_id={Uri.EscapeDataString(filter.StatusId)}");
            }
        }

        if (!string.IsNullOrEmpty(filter.ProjectId))
        {
            queryParams.Add($"project_id={Uri.EscapeDataString(filter.ProjectId)}");
        }

        if (filter.Limit.HasValue)
        {
            queryParams.Add($"per_page={filter.Limit.Value}");
        }

        if (filter.Offset.HasValue)
        {
            var page = (filter.Offset.Value / (filter.Limit ?? 25)) + 1;
            queryParams.Add($"page={page}");
        }

        if (queryParams.Count > 0)
        {
            url += "?" + string.Join("&", queryParams);
        }

        return url;
    }

    private static void OpenInBrowser(string url)
    {
        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Process.Start(new ProcessStartInfo("cmd", $"/c start {url.Replace("&", "^&")}") { CreateNoWindow = true });
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                Process.Start("open", url);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                Process.Start("xdg-open", url);
            }
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[yellow]Warning: Could not open browser automatically. Please open this URL manually: {url}[/]");
            AnsiConsole.MarkupLine($"[dim]Error: {ex.Message}[/]");
        }
    }
}