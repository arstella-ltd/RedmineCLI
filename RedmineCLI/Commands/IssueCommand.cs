using System.CommandLine;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Threading;

using Microsoft.Extensions.Logging;

using RedmineCLI.Exceptions;
using RedmineCLI.Formatters;
using RedmineCLI.Models;
using RedmineCLI.Services;

using Spectre.Console;

namespace RedmineCLI.Commands;

public class IssueCommand
{
    private readonly IRedmineService _redmineService;
    private readonly IConfigService _configService;
    private readonly ITableFormatter _tableFormatter;
    private readonly IJsonFormatter _jsonFormatter;
    private readonly ILogger<IssueCommand> _logger;

    public IssueCommand(
        IRedmineService redmineService,
        IConfigService configService,
        ITableFormatter tableFormatter,
        IJsonFormatter jsonFormatter,
        ILogger<IssueCommand> logger)
    {
        _redmineService = redmineService;
        _configService = configService;
        _tableFormatter = tableFormatter;
        _jsonFormatter = jsonFormatter;
        _logger = logger;
    }

    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(IssueCommand))]
    public static Command Create(
        IRedmineService redmineService,
        IConfigService configService,
        ITableFormatter tableFormatter,
        IJsonFormatter jsonFormatter,
        ILogger<IssueCommand> logger)
    {
        var command = new Command("issue", "Manage Redmine issues");
        var issueCommand = new IssueCommand(redmineService, configService, tableFormatter, jsonFormatter, logger);

        var listCommand = new Command("list", "List issues with optional filters");
        listCommand.Aliases.Add("ls");

        var assigneeOption = new Option<string?>("--assignee") { Description = "Filter by assignee (username, ID, or @me)" };
        assigneeOption.Aliases.Add("-a");
        var statusOption = new Option<string?>("--status") { Description = "Filter by status (open, closed, all, or status ID)" };
        statusOption.Aliases.Add("-s");
        var projectOption = new Option<string?>("--project") { Description = "Filter by project (identifier or ID)" };
        projectOption.Aliases.Add("-p");
        var priorityOption = new Option<string?>("--priority") { Description = "Filter by priority (name or ID)" };
        var limitOption = new Option<int?>("--limit") { Description = "Limit number of results (default: 30)" };
        limitOption.Aliases.Add("-L");
        var offsetOption = new Option<int?>("--offset") { Description = "Offset for pagination" };
        var jsonOption = new Option<bool>("--json") { Description = "Output in JSON format" };
        var webOption = new Option<bool>("--web") { Description = "Open in web browser" };
        webOption.Aliases.Add("-w");
        var absoluteTimeOption = new Option<bool>("--absolute-time") { Description = "Display absolute time instead of relative time" };
        var searchOption = new Option<string?>("--search") { Description = "Search in issue titles and descriptions" };
        searchOption.Aliases.Add("-q");
        var sortOption = new Option<string?>("--sort") { Description = "Sort by field (e.g., updated_on:desc, priority:desc,id)" };

        listCommand.Add(assigneeOption);
        listCommand.Add(statusOption);
        listCommand.Add(projectOption);
        listCommand.Add(priorityOption);
        listCommand.Add(limitOption);
        listCommand.Add(offsetOption);
        listCommand.Add(jsonOption);
        listCommand.Add(webOption);
        listCommand.Add(absoluteTimeOption);
        listCommand.Add(searchOption);
        listCommand.Add(sortOption);

        listCommand.SetAction(async (parseResult) =>
        {
            var assignee = parseResult.GetValue(assigneeOption);
            var status = parseResult.GetValue(statusOption);
            var project = parseResult.GetValue(projectOption);
            var priority = parseResult.GetValue(priorityOption);
            var limit = parseResult.GetValue(limitOption);
            var offset = parseResult.GetValue(offsetOption);
            var json = parseResult.GetValue(jsonOption);
            var web = parseResult.GetValue(webOption);
            var absoluteTime = parseResult.GetValue(absoluteTimeOption);
            var search = parseResult.GetValue(searchOption);
            var sort = parseResult.GetValue(sortOption);

            var options = new IssueListOptions
            {
                Assignee = assignee,
                Status = status,
                Project = project,
                Limit = limit,
                Offset = offset,
                Json = json,
                Web = web,
                AbsoluteTime = absoluteTime,
                Search = search,
                Sort = sort,
                Priority = priority
            };

            Environment.ExitCode = await issueCommand.ListAsync(options, CancellationToken.None);
        });

        command.Add(listCommand);

        // View command
        var viewCommand = new Command("view", "View issue details");
        var idArgument = new Argument<int>("ID");
        idArgument.Description = "Issue ID";
        var viewJsonOption = new Option<bool>("--json") { Description = "Output in JSON format" };
        var viewWebOption = new Option<bool>("--web") { Description = "Open in web browser" };
        viewWebOption.Aliases.Add("-w");
        var viewAbsoluteTimeOption = new Option<bool>("--absolute-time") { Description = "Display absolute time instead of relative time" };
        var viewImageOption = new Option<bool>("--image") { Description = "Display inline images using Sixel protocol" };

        viewCommand.Add(idArgument);
        viewCommand.Add(viewJsonOption);
        viewCommand.Add(viewWebOption);
        viewCommand.Add(viewAbsoluteTimeOption);
        viewCommand.Add(viewImageOption);

        viewCommand.SetAction(async (parseResult) =>
        {
            var id = parseResult.GetValue(idArgument);
            var json = parseResult.GetValue(viewJsonOption);
            var web = parseResult.GetValue(viewWebOption);
            var absoluteTime = parseResult.GetValue(viewAbsoluteTimeOption);
            var showImages = parseResult.GetValue(viewImageOption);

            Environment.ExitCode = await issueCommand.ViewAsync(id, json, web, absoluteTime, showImages, CancellationToken.None);
        });

        command.Add(viewCommand);

        // Create command
        var createCommand = new Command("create", "Create a new issue");
        var createProjectOption = new Option<string?>("--project") { Description = "Project identifier or ID" };
        createProjectOption.Aliases.Add("-p");
        var createTitleOption = new Option<string?>("--title") { Description = "Issue title/subject" };
        createTitleOption.Aliases.Add("-t");
        var createDescriptionOption = new Option<string?>("--description") { Description = "Issue description" };
        createDescriptionOption.Aliases.Add("-d");
        var createAssigneeOption = new Option<string?>("--assignee") { Description = "Assignee (username, ID, or @me)" };
        createAssigneeOption.Aliases.Add("-a");
        var createWebOption = new Option<bool>("--web") { Description = "Open new issue page in web browser" };
        createWebOption.Aliases.Add("-w");

        createCommand.Add(createProjectOption);
        createCommand.Add(createTitleOption);
        createCommand.Add(createDescriptionOption);
        createCommand.Add(createAssigneeOption);
        createCommand.Add(createWebOption);

        createCommand.SetAction(async (parseResult) =>
        {
            var project = parseResult.GetValue(createProjectOption);
            var title = parseResult.GetValue(createTitleOption);
            var description = parseResult.GetValue(createDescriptionOption);
            var assignee = parseResult.GetValue(createAssigneeOption);
            var web = parseResult.GetValue(createWebOption);

            Environment.ExitCode = await issueCommand.CreateAsync(project, title, description, assignee, web, CancellationToken.None);
        });

        command.Add(createCommand);

        // Edit command
        var editCommand = new Command("edit", "Edit an existing issue");
        var editIdArgument = new Argument<int>("ID");
        editIdArgument.Description = "Issue ID";
        var editStatusOption = new Option<string?>("--status") { Description = "New status" };
        editStatusOption.Aliases.Add("-s");
        var editAssigneeOption = new Option<string?>("--assignee") { Description = "New assignee (username, ID, or @me)" };
        editAssigneeOption.Aliases.Add("-a");
        var editDoneRatioOption = new Option<int?>("--done-ratio") { Description = "Progress percentage (0-100)" };
        var editWebOption = new Option<bool>("--web") { Description = "Open edit page in web browser" };
        editWebOption.Aliases.Add("-w");

        editCommand.Add(editIdArgument);
        editCommand.Add(editStatusOption);
        editCommand.Add(editAssigneeOption);
        editCommand.Add(editDoneRatioOption);
        editCommand.Add(editWebOption);

        editCommand.SetAction(async (parseResult) =>
        {
            var id = parseResult.GetValue(editIdArgument);
            var status = parseResult.GetValue(editStatusOption);
            var assignee = parseResult.GetValue(editAssigneeOption);
            var doneRatio = parseResult.GetValue(editDoneRatioOption);
            var web = parseResult.GetValue(editWebOption);

            Environment.ExitCode = await issueCommand.EditAsync(id, status, assignee, doneRatio, web, CancellationToken.None);
        });

        command.Add(editCommand);

        // Comment command
        var commentCommand = new Command("comment", "Add a comment to an issue");
        var commentIdArgument = new Argument<int>("ID");
        commentIdArgument.Description = "Issue ID";
        var commentMessageOption = new Option<string?>("--message") { Description = "Comment text" };
        commentMessageOption.Aliases.Add("-m");

        commentCommand.Add(commentIdArgument);
        commentCommand.Add(commentMessageOption);

        commentCommand.SetAction(async (parseResult) =>
        {
            var id = parseResult.GetValue(commentIdArgument);
            var message = parseResult.GetValue(commentMessageOption);

            Environment.ExitCode = await issueCommand.CommentAsync(id, message, CancellationToken.None);
        });

        command.Add(commentCommand);

        // Close command
        var closeCommand = new Command("close", "Close issues");
        var closeIdsArgument = new Argument<int[]>("IDs");
        closeIdsArgument.Description = "Issue IDs to close";
        closeIdsArgument.Arity = ArgumentArity.OneOrMore;
        var closeMessageOption = new Option<string?>("--message") { Description = "Add a comment when closing" };
        closeMessageOption.Aliases.Add("-m");
        var closeStatusOption = new Option<string?>("--status") { Description = "Specific status to use for closing" };
        closeStatusOption.Aliases.Add("-s");
        var closeDoneRatioOption = new Option<int?>("--done-ratio") { Description = "Progress percentage (default: 100)" };
        closeDoneRatioOption.Aliases.Add("-d");
        var closeJsonOption = new Option<bool>("--json") { Description = "Output in JSON format" };

        closeCommand.Add(closeIdsArgument);
        closeCommand.Add(closeMessageOption);
        closeCommand.Add(closeStatusOption);
        closeCommand.Add(closeDoneRatioOption);
        closeCommand.Add(closeJsonOption);

        closeCommand.SetAction(async (parseResult) =>
        {
            var ids = parseResult.GetValue(closeIdsArgument) ?? Array.Empty<int>();
            var message = parseResult.GetValue(closeMessageOption);
            var status = parseResult.GetValue(closeStatusOption);
            var doneRatio = parseResult.GetValue(closeDoneRatioOption);
            var json = parseResult.GetValue(closeJsonOption);

            Environment.ExitCode = await issueCommand.CloseAsync(ids, message, status, doneRatio, json, CancellationToken.None);
        });

        command.Add(closeCommand);

        // Attachment command
        var attachmentCommand = new Command("attachment", "Manage issue attachments");

        // Attachment list subcommand
        var attachmentListCommand = new Command("list", "List attachments of an issue");
        var attachmentListIdArgument = new Argument<int>("ID");
        attachmentListIdArgument.Description = "Issue ID";
        var attachmentListJsonOption = new Option<bool>("--json") { Description = "Output in JSON format" };

        attachmentListCommand.Add(attachmentListIdArgument);
        attachmentListCommand.Add(attachmentListJsonOption);

        attachmentListCommand.SetAction(async (parseResult) =>
        {
            var id = parseResult.GetValue(attachmentListIdArgument);
            var json = parseResult.GetValue(attachmentListJsonOption);

            Environment.ExitCode = await issueCommand.ListAttachmentsAsync(id, json, CancellationToken.None);
        });

        attachmentCommand.Add(attachmentListCommand);

        // Attachment download subcommand
        var attachmentDownloadCommand = new Command("download", "Download attachments from an issue");
        var attachmentDownloadIdArgument = new Argument<int>("ID");
        attachmentDownloadIdArgument.Description = "Issue ID";
        var attachmentDownloadAllOption = new Option<bool>("--all") { Description = "Download all attachments" };
        var attachmentDownloadOutputOption = new Option<string?>("--output") { Description = "Output directory (default: current directory)" };
        attachmentDownloadOutputOption.Aliases.Add("-o");

        attachmentDownloadCommand.Add(attachmentDownloadIdArgument);
        attachmentDownloadCommand.Add(attachmentDownloadAllOption);
        attachmentDownloadCommand.Add(attachmentDownloadOutputOption);

        attachmentDownloadCommand.SetAction(async (parseResult) =>
        {
            var id = parseResult.GetValue(attachmentDownloadIdArgument);
            var all = parseResult.GetValue(attachmentDownloadAllOption);
            var output = parseResult.GetValue(attachmentDownloadOutputOption);

            Environment.ExitCode = await issueCommand.DownloadAttachmentsAsync(id, all, output, CancellationToken.None);
        });

        attachmentCommand.Add(attachmentDownloadCommand);
        command.Add(attachmentCommand);

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
        bool absoluteTime,
        string? search,
        string? sort,
        string? priority,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Listing issues with filters - Assignee: {Assignee}, Status: {Status}, Project: {Project}, Priority: {Priority}, Search: {Search}, Sort: {Sort}",
                assignee, status, project, priority, search, sort);

            // Handle @me special value
            assignee = await ResolveAssigneeAsync(assignee, cancellationToken);

            // Handle project name to identifier resolution
            project = await ResolveProjectAsync(project, cancellationToken);

            // Handle status resolution
            string? statusFilter = await ResolveStatusAsync(status, cancellationToken);

            // Handle priority resolution
            string? priorityFilter = await ResolvePriorityAsync(priority, cancellationToken);

            // Validate sort parameter if provided
            if (!string.IsNullOrEmpty(sort))
            {
                if (!IsValidSortParameter(sort))
                {
                    AnsiConsole.MarkupLine($"[red]Error:[/] Invalid sort parameter: {sort}");
                    AnsiConsole.MarkupLine("[dim]Valid fields: id, subject, status, priority, author, assigned_to, updated_on, created_on, start_date, due_date, done_ratio, category, fixed_version[/]");
                    AnsiConsole.MarkupLine("[dim]Format: field or field:asc or field:desc (e.g., updated_on:desc or priority:desc,id)[/]");
                    return 1;
                }
            }

            List<Issue> issues;

            // Use search API if search parameter is provided
            if (!string.IsNullOrEmpty(search))
            {
                // Handle --web option for search
                if (web)
                {
                    return await OpenInBrowserAsync(
                        profile => BuildSearchUrl(profile.Url, search, assignee, statusFilter, project),
                        "search results",
                        cancellationToken);
                }

                issues = await _redmineService.SearchIssuesAsync(
                    search,
                    assignee,
                    statusFilter,
                    project,
                    limit ?? 30,
                    offset,
                    sort,
                    cancellationToken);
            }
            else
            {
                var filter = new IssueFilter
                {
                    AssignedToId = assignee,
                    StatusId = statusFilter,
                    ProjectId = project,
                    PriorityId = priorityFilter,
                    Limit = limit ?? 30, // Default limit to 30
                    Offset = offset,
                    Sort = sort
                };

                // If no filters are specified, default to open issues
                if (string.IsNullOrEmpty(assignee) && string.IsNullOrEmpty(status) && string.IsNullOrEmpty(project) && string.IsNullOrEmpty(priority))
                {
                    filter.StatusId = "open";
                }

                // Handle --web option for normal list
                if (web)
                {
                    return await OpenInBrowserAsync(
                        profile => BuildIssuesUrl(profile.Url, filter),
                        "issues",
                        cancellationToken);
                }

                issues = await _redmineService.GetIssuesAsync(filter, cancellationToken);
            }

            if (json)
            {
                _jsonFormatter.FormatIssues(issues);
            }
            else
            {
                // Determine time format
                TimeFormat timeFormat = TimeFormat.Relative;

                if (absoluteTime)
                {
                    timeFormat = TimeFormat.Absolute;
                }
                else
                {
                    // Check config setting
                    var config = await _configService.LoadConfigAsync();
                    if (config.Preferences?.Time?.Format != null)
                    {
                        timeFormat = config.Preferences.Time.Format.ToLower() switch
                        {
                            "absolute" => TimeFormat.Absolute,
                            "utc" => TimeFormat.Utc,
                            _ => TimeFormat.Relative
                        };
                    }
                }

                _tableFormatter.SetTimeFormat(timeFormat);
                _tableFormatter.FormatIssues(issues);
            }

            return 0;
        }
        catch (ValidationException ex)
        {
            _logger.LogError(ex, "Validation error while listing issues");
            AnsiConsole.MarkupLine($"[red]Error:[/] {ex.Message}");
            return 1;
        }
        catch (RedmineApiException ex)
        {
            _logger.LogError(ex, "API error while listing issues");
            AnsiConsole.MarkupLine($"[red]Error:[/] {ex.Message}");
            return 1;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while listing issues");
            AnsiConsole.MarkupLine($"[red]Error:[/] {ex.Message}");
            return 1;
        }
    }

    /// <summary>
    /// チケット一覧を表示する（新しいオーバーロード）
    /// </summary>
    /// <param name="options">一覧表示オプション</param>
    /// <param name="cancellationToken">キャンセルトークン</param>
    /// <returns>終了コード</returns>
    public async Task<int> ListAsync(IssueListOptions options, CancellationToken cancellationToken)
    {
        // 既存のListAsyncメソッドを呼び出す
        return await ListAsync(
            options.Assignee,
            options.Status,
            options.Project,
            options.Limit,
            options.Offset,
            options.Json,
            options.Web,
            options.AbsoluteTime,
            options.Search,
            options.Sort,
            options.Priority,
            cancellationToken);
    }

    private static string BuildIssuesUrl(string baseUrl, IssueFilter filter)
    {
        var url = $"{baseUrl.TrimEnd('/')}/issues";
        var queryParams = new List<string>();

        // Add set_filter=1 if any filter is specified
        bool hasFilter = !string.IsNullOrEmpty(filter.AssignedToId) ||
                        !string.IsNullOrEmpty(filter.StatusId) ||
                        !string.IsNullOrEmpty(filter.ProjectId);

        if (hasFilter)
        {
            queryParams.Add("set_filter=1");
        }

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

    private static string BuildSearchUrl(string baseUrl, string searchQuery, string? assignee, string? status, string? project)
    {
        var url = $"{baseUrl.TrimEnd('/')}/search";
        var queryParams = new List<string>
        {
            $"q={Uri.EscapeDataString(searchQuery)}",
            "issues=1", // Search in issues
            "titles_only=0" // Search in titles and descriptions
        };

        // Add filters if specified
        if (!string.IsNullOrEmpty(assignee))
        {
            queryParams.Add($"assigned_to_id={Uri.EscapeDataString(assignee)}");
        }

        if (!string.IsNullOrEmpty(status))
        {
            if (status == "open")
            {
                queryParams.Add("open_issues=1");
            }
            else if (status == "closed")
            {
                queryParams.Add("open_issues=0");
            }
        }

        if (!string.IsNullOrEmpty(project))
        {
            queryParams.Add($"projects={Uri.EscapeDataString(project)}");
        }

        url += "?" + string.Join("&", queryParams);
        return url;
    }

    private static void OpenInBrowser(string url)
    {
        try
        {
            // Check if running under test runner
            var isRunningInTest = AppDomain.CurrentDomain.GetAssemblies()
                .Any(a => a.FullName?.Contains("testhost") == true ||
                         a.FullName?.Contains("Microsoft.TestPlatform") == true);

            // Check if browser opening is disabled (for testing)
            var disableBrowser = Environment.GetEnvironmentVariable("REDMINE_CLI_DISABLE_BROWSER");
            if (isRunningInTest ||
                (!string.IsNullOrEmpty(disableBrowser) &&
                (disableBrowser.Equals("true", StringComparison.OrdinalIgnoreCase) ||
                 disableBrowser.Equals("1", StringComparison.OrdinalIgnoreCase))))
            {
                // Just log the URL instead of opening browser
                Console.WriteLine($"Browser opening disabled. URL: {url}");
                return;
            }

            // First, check if BROWSER environment variable is set
            var browserCommand = Environment.GetEnvironmentVariable("BROWSER");
            if (!string.IsNullOrEmpty(browserCommand))
            {
                // Replace %s with the URL, or append URL if %s is not present
                if (browserCommand.Contains("%s"))
                {
                    browserCommand = browserCommand.Replace("%s", url);
                    var parts = browserCommand.Split(' ', 2);
                    if (parts.Length > 1)
                    {
                        Process.Start(parts[0], parts[1]);
                    }
                    else
                    {
                        Process.Start(parts[0]);
                    }
                }
                else
                {
                    Process.Start(browserCommand, url);
                }
                return;
            }

            // Fallback to platform-specific commands
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

    private async Task<int> OpenInBrowserAsync(
        Func<Models.Profile, string> urlBuilder,
        string resourceType,
        CancellationToken cancellationToken)
    {
        var profile = await _configService.GetActiveProfileAsync();
        if (profile == null)
        {
            AnsiConsole.MarkupLine("[red]Error:[/] No active profile found. Please run 'redmine auth login' first.");
            return 1;
        }

        var url = urlBuilder(profile);
        _logger.LogDebug("Opening {ResourceType} in browser: {Url}", resourceType, url);
        OpenInBrowser(url);
        AnsiConsole.MarkupLine($"[green]Opening {resourceType} in browser: {Markup.Escape(url)}[/]");
        return 0;
    }

    public async Task<int> ViewAsync(int issueId, bool json, bool web, bool absoluteTime, bool showImages, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Viewing issue {IssueId}", issueId);

            // Handle web option
            if (web)
            {
                return await OpenInBrowserAsync(
                    profile => $"{profile.Url.TrimEnd('/')}/issues/{issueId}",
                    "issue",
                    cancellationToken);
            }

            // Fetch issue details with journals
            var issue = await _redmineService.GetIssueAsync(issueId, true, cancellationToken);

            // Format output
            if (json)
            {
                _jsonFormatter.FormatIssueDetails(issue);
            }
            else
            {
                // Determine time format
                TimeFormat timeFormat = TimeFormat.Relative;

                if (absoluteTime)
                {
                    timeFormat = TimeFormat.Absolute;
                }
                else
                {
                    // Check config setting
                    var config = await _configService.LoadConfigAsync();
                    if (config.Preferences?.Time?.Format != null)
                    {
                        timeFormat = config.Preferences.Time.Format.ToLower() switch
                        {
                            "absolute" => TimeFormat.Absolute,
                            "utc" => TimeFormat.Utc,
                            _ => TimeFormat.Relative
                        };
                    }
                }

                _tableFormatter.SetTimeFormat(timeFormat);
                _tableFormatter.FormatIssueDetails(issue, showImages);
            }

            return 0;
        }
        catch (RedmineApiException ex)
        {
            _logger.LogError(ex, "API error while viewing issue {IssueId}", issueId);

            if (ex.StatusCode == 404)
            {
                AnsiConsole.MarkupLine($"[red]Error:[/] Issue #{issueId} not found.");
            }
            else
            {
                AnsiConsole.MarkupLine($"[red]Error:[/] {ex.Message}");
            }
            return 1;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while viewing issue {IssueId}", issueId);
            AnsiConsole.MarkupLine($"[red]Error:[/] {ex.Message}");
            return 1;
        }
    }

    public async Task<int> CreateAsync(
        string? project,
        string? title,
        string? description,
        string? assignee,
        bool web,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Creating issue - Project: {Project}, Title: {Title}, Web: {Web}", project, title, web);

            // Handle --web option
            if (web)
            {
                return await OpenInBrowserAsync(
                    profile => BuildNewIssueUrl(profile.Url, project),
                    "new issue page",
                    cancellationToken);
            }

            // Interactive mode if no options provided
            if (string.IsNullOrEmpty(project) && string.IsNullOrEmpty(title))
            {
                return await CreateInteractiveAsync(cancellationToken);
            }

            // Validate required fields
            if (string.IsNullOrEmpty(title))
            {
                AnsiConsole.MarkupLine("[red]Error:[/] Title is required");
                return 1;
            }

            // Validate required fields
            if (string.IsNullOrEmpty(project))
            {
                AnsiConsole.MarkupLine("[red]Error:[/] Project is required");
                return 1;
            }

            // Create the issue using RedmineService
            var createdIssue = await _redmineService.CreateIssueAsync(
                project,
                title,
                description,
                assignee,
                cancellationToken);

            // Show success message with URL
            var profile = await _configService.GetActiveProfileAsync();
            if (profile != null)
            {
                var issueUrl = $"{profile.Url.TrimEnd('/')}/issues/{createdIssue.Id}";
                AnsiConsole.MarkupLine($"[green]✓[/] Issue #{createdIssue.Id} created: {Markup.Escape(createdIssue.Subject)}");
                AnsiConsole.MarkupLine($"[dim]View at: {Markup.Escape(issueUrl)}[/]");
            }
            else
            {
                AnsiConsole.MarkupLine($"[green]✓[/] Issue #{createdIssue.Id} created: {Markup.Escape(createdIssue.Subject)}");
            }

            return 0;
        }
        catch (ValidationException ex)
        {
            _logger.LogError(ex, "Validation error while creating issue");
            AnsiConsole.MarkupLine($"[red]Error:[/] {ex.Message}");
            return 1;
        }
        catch (RedmineApiException ex)
        {
            _logger.LogError(ex, "API error while creating issue");
            AnsiConsole.MarkupLine($"[red]Error:[/] {ex.Message}");
            return 1;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while creating issue");
            AnsiConsole.MarkupLine($"[red]Error:[/] {ex.Message}");
            return 1;
        }
    }

    // Overload for tests that use different signatures
    public Task<int> CreateAsync(int projectId, string title, string? description, string? assignee, CancellationToken cancellationToken)
    {
        return CreateAsync(projectId.ToString(), title, description, assignee, false, cancellationToken);
    }

    private async Task<int> CreateInteractiveAsync(CancellationToken cancellationToken)
    {
        try
        {
            AnsiConsole.MarkupLine("[bold cyan]Create New Issue[/]");
            AnsiConsole.WriteLine();

            // Get projects list
            var projects = await _redmineService.GetProjectsAsync(cancellationToken);
            if (projects.Count == 0)
            {
                AnsiConsole.MarkupLine("[red]Error:[/] No projects available");
                return 1;
            }

            // Project selection
            var selectedProject = AnsiConsole.Prompt(
                new SelectionPrompt<Project>()
                    .Title("Select a project:")
                    .PageSize(10)
                    .MoreChoicesText("[grey](Move up and down to reveal more projects)[/]")
                    .UseConverter(p => $"{p.Name} ({p.Identifier})")
                    .AddChoices(projects));

            // Title input
            var title = AnsiConsole.Prompt(
                new TextPrompt<string>("Enter issue title:")
                    .ValidationErrorMessage("[red]Title cannot be empty[/]")
                    .Validate(input =>
                    {
                        if (string.IsNullOrWhiteSpace(input))
                            return ValidationResult.Error("Title cannot be empty");
                        return ValidationResult.Success();
                    }));

            // Description input (optional)
            var description = AnsiConsole.Prompt(
                new TextPrompt<string>("Enter description [dim](optional)[/]:")
                    .AllowEmpty());

            // Assignee input (optional)
            var assignToMe = AnsiConsole.Confirm("Assign to yourself?", false);

            // Create the issue
            var issue = new Issue
            {
                Subject = title,
                Description = string.IsNullOrWhiteSpace(description) ? null : description,
                Project = selectedProject
            };

            if (assignToMe)
            {
                var currentUser = await _redmineService.GetCurrentUserAsync(cancellationToken);
                issue.AssignedTo = currentUser;
            }

            var createdIssue = await _redmineService.CreateIssueAsync(
                issue.Project.Identifier ?? issue.Project.Id.ToString(),
                issue.Subject,
                issue.Description,
                assignToMe ? "@me" : null,
                cancellationToken);

            // Show success message with URL
            var profile = await _configService.GetActiveProfileAsync();
            if (profile != null)
            {
                var issueUrl = $"{profile.Url.TrimEnd('/')}/issues/{createdIssue.Id}";
                AnsiConsole.MarkupLine($"[green]✓[/] Issue #{createdIssue.Id} created: {Markup.Escape(createdIssue.Subject)}");
                AnsiConsole.MarkupLine($"[dim]View at: {Markup.Escape(issueUrl)}[/]");
            }
            else
            {
                AnsiConsole.MarkupLine($"[green]✓[/] Issue #{createdIssue.Id} created: {Markup.Escape(createdIssue.Subject)}");
            }

            return 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during interactive issue creation");
            AnsiConsole.MarkupLine($"[red]Error:[/] {ex.Message}");
            return 1;
        }
    }

    private static string BuildNewIssueUrl(string baseUrl, string? project)
    {
        var url = $"{baseUrl.TrimEnd('/')}/issues/new";

        if (!string.IsNullOrEmpty(project))
        {
            // If project is specified, add it as a query parameter
            url += $"?issue[project_id]={Uri.EscapeDataString(project)}";
        }

        return url;
    }

    private async Task<string?> ResolveAssigneeAsync(string? assignee, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(assignee))
            return null;

        if (assignee == "@me")
        {
            var currentUser = await _redmineService.GetCurrentUserAsync(cancellationToken);
            return currentUser.Id.ToString();
        }

        // 数値の場合はそのままIDとして返す
        if (int.TryParse(assignee, out _))
        {
            return assignee;
        }

        // 文字列の場合はユーザー名として扱い、ユーザーIDを検索する
        try
        {
            var users = await _redmineService.GetUsersAsync(null, cancellationToken);
            var matchedUser = users.FirstOrDefault(u =>
                u.DisplayName.Equals(assignee, StringComparison.OrdinalIgnoreCase) ||
                u.Login?.Equals(assignee, StringComparison.OrdinalIgnoreCase) == true ||
                (!string.IsNullOrEmpty(u.FirstName) && !string.IsNullOrEmpty(u.LastName) &&
                 ($"{u.FirstName} {u.LastName}".Equals(assignee, StringComparison.OrdinalIgnoreCase) ||
                  $"{u.LastName} {u.FirstName}".Equals(assignee, StringComparison.OrdinalIgnoreCase))));

            if (matchedUser != null)
            {
                _logger.LogDebug("Resolved assignee '{Assignee}' to user ID {UserId}", assignee, matchedUser.Id);
                return matchedUser.Id.ToString();
            }

            // ユーザーが見つからない場合はエラーをスロー
            _logger.LogError("Could not find user with name '{Assignee}'", assignee);
            throw new ValidationException($"User '{assignee}' not found.");
        }
        catch (ValidationException)
        {
            // ValidationExceptionはそのまま再スロー
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to resolve assignee '{Assignee}'", assignee);
            throw new ValidationException($"Failed to resolve user '{assignee}': {ex.Message}", ex);
        }
    }

    private async Task<string?> ResolveStatusAsync(string? status, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(status))
            return null;

        // Handle special keywords
        if (status.Equals("all", StringComparison.OrdinalIgnoreCase))
        {
            return "*"; // Use * to get all statuses per Redmine API
        }

        if (status.Equals("open", StringComparison.OrdinalIgnoreCase) ||
            status.Equals("closed", StringComparison.OrdinalIgnoreCase))
        {
            return status.ToLower();
        }

        // If numeric, validate it's a valid status ID
        if (int.TryParse(status, out var statusId))
        {
            try
            {
                var statuses = await _redmineService.GetIssueStatusesAsync(cancellationToken);
                if (statuses.Any(s => s.Id == statusId))
                {
                    return status;
                }
                else
                {
                    throw new ValidationException($"Status ID '{status}' not found.");
                }
            }
            catch (ValidationException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to validate status ID '{StatusId}'", status);
                throw new ValidationException($"Failed to validate status ID '{status}': {ex.Message}", ex);
            }
        }

        // Try to match by status name
        try
        {
            var statuses = await _redmineService.GetIssueStatusesAsync(cancellationToken);
            var matchedStatus = statuses.FirstOrDefault(s =>
                s.Name.Equals(status, StringComparison.OrdinalIgnoreCase));

            if (matchedStatus != null)
            {
                _logger.LogDebug("Resolved status '{Status}' to ID {StatusId}", status, matchedStatus.Id);
                return matchedStatus.Id.ToString();
            }

            // Status not found
            _logger.LogError("Could not find status with name '{Status}'", status);
            throw new ValidationException($"Status '{status}' not found.");
        }
        catch (ValidationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to resolve status '{Status}'", status);
            throw new ValidationException($"Failed to resolve status '{status}': {ex.Message}", ex);
        }
    }

    private async Task<string?> ResolvePriorityAsync(string? priority, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(priority))
            return null;

        // If numeric, validate it's a valid priority ID
        if (int.TryParse(priority, out var priorityId))
        {
            try
            {
                var priorities = await _redmineService.GetPrioritiesAsync(cancellationToken);
                if (priorities.Any(p => p.Id == priorityId))
                {
                    return priority;
                }
                else
                {
                    throw new ValidationException($"Priority ID '{priority}' not found.");
                }
            }
            catch (ValidationException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to validate priority ID '{PriorityId}'", priority);
                throw new ValidationException($"Failed to validate priority ID '{priority}': {ex.Message}", ex);
            }
        }

        // Try to match by priority name
        try
        {
            var priorities = await _redmineService.GetPrioritiesAsync(cancellationToken);
            var matchedPriority = priorities.FirstOrDefault(p =>
                p.Name.Equals(priority, StringComparison.OrdinalIgnoreCase));

            if (matchedPriority != null)
            {
                _logger.LogDebug("Resolved priority '{Priority}' to ID {PriorityId}", priority, matchedPriority.Id);
                return matchedPriority.Id.ToString();
            }

            // Priority not found
            _logger.LogError("Could not find priority with name '{Priority}'", priority);
            throw new ValidationException($"Priority '{priority}' not found.");
        }
        catch (ValidationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to resolve priority '{Priority}'", priority);
            throw new ValidationException($"Failed to resolve priority '{priority}': {ex.Message}", ex);
        }
    }

    private async Task<(string? StatusId, IssueStatus? Status)> ResolveStatusForEditAsync(string? status, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(status))
            return (null, null);

        // Handle special keywords - not allowed for editing (exact match only)
        if (status == "all" || status == "open" || status == "closed")
        {
            throw new ValidationException($"Cannot directly set status to '{status}'. Please specify a specific status name or ID.");
        }

        try
        {
            var statuses = await _redmineService.GetIssueStatusesAsync(cancellationToken);

            // If numeric, validate it's a valid status ID
            if (int.TryParse(status, out var statusId))
            {
                var matchedStatus = statuses.FirstOrDefault(s => s.Id == statusId);
                if (matchedStatus != null)
                {
                    return (status, matchedStatus);
                }
                else
                {
                    throw new ValidationException($"Status ID '{status}' not found.");
                }
            }

            // Try to match by status name
            var statusByName = statuses.FirstOrDefault(s =>
                s.Name.Equals(status, StringComparison.OrdinalIgnoreCase));

            if (statusByName != null)
            {
                _logger.LogDebug("Resolved status '{Status}' to ID {StatusId}", status, statusByName.Id);
                return (statusByName.Id.ToString(), statusByName);
            }

            // Status not found
            _logger.LogError("Could not find status with name '{Status}'", status);
            throw new ValidationException($"Status '{status}' not found.");
        }
        catch (ValidationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to resolve status '{Status}'", status);
            throw new ValidationException($"Failed to resolve status '{status}': {ex.Message}", ex);
        }
    }

    private static User? ParseAssignee(string? assignee)
    {
        if (string.IsNullOrEmpty(assignee))
            return null;

        if (int.TryParse(assignee, out var assigneeId))
        {
            return new User { Id = assigneeId };
        }

        return new User { Name = assignee };
    }

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

    private static Project? ParseProject(string? project)
    {
        if (string.IsNullOrEmpty(project))
            return null;

        if (int.TryParse(project, out var projectId))
        {
            return new Project { Id = projectId };
        }

        return new Project { Identifier = project };
    }

    public async Task<int> EditAsync(
        int issueId,
        string? status,
        string? assignee,
        int? doneRatio,
        bool web,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Editing issue {IssueId} - Status: {Status}, Assignee: {Assignee}, DoneRatio: {DoneRatio}, Web: {Web}",
                issueId, status, assignee, doneRatio, web);

            // Handle --web option
            if (web)
            {
                return await OpenInBrowserAsync(
                    profile => $"{profile.Url.TrimEnd('/')}/issues/{issueId}/edit",
                    "issue edit page",
                    cancellationToken);
            }

            // Validate done ratio
            if (doneRatio.HasValue && (doneRatio.Value < 0 || doneRatio.Value > 100))
            {
                AnsiConsole.MarkupLine("[red]Error:[/] Done ratio must be between 0 and 100");
                return 1;
            }

            // If no options provided, launch interactive mode
            if (string.IsNullOrEmpty(status) && string.IsNullOrEmpty(assignee) && !doneRatio.HasValue)
            {
                return await EditInteractiveAsync(issueId, cancellationToken);
            }

            // Fetch current issue to get the subject
            var currentIssue = await _redmineService.GetIssueAsync(issueId, false, cancellationToken);

            // Track what's being updated for display
            var fieldsToUpdate = new List<string>();
            var updateDetails = new Dictionary<string, string>();

            // Handle status
            string? statusIdOrName = null;
            if (!string.IsNullOrEmpty(status))
            {
                try
                {
                    var (statusId, statusObj) = await ResolveStatusForEditAsync(status, cancellationToken);
                    if (statusObj != null)
                    {
                        statusIdOrName = status; // Pass the original status string to RedmineService
                        fieldsToUpdate.Add("status");
                        updateDetails["status"] = statusObj.Name;
                    }
                }
                catch (ValidationException ex)
                {
                    AnsiConsole.MarkupLine($"[red]Error:[/] {ex.Message}");
                    return 1;
                }
            }

            // Handle assignee
            string? assigneeIdOrUsername = null;
            if (!string.IsNullOrEmpty(assignee))
            {
                assigneeIdOrUsername = assignee; // Let RedmineService handle the resolution
                fieldsToUpdate.Add("assignee");
                var resolvedAssignee = await ResolveAssigneeAsync(assignee, cancellationToken);
                updateDetails["assignee"] = resolvedAssignee ?? assignee;
            }

            // Handle done ratio
            if (doneRatio.HasValue)
            {
                fieldsToUpdate.Add("progress");
                updateDetails["progress"] = $"{doneRatio.Value}%";
            }

            // Log what we're updating
            _logger.LogDebug("Updating issue {IssueId} fields: {Fields}", issueId, string.Join(", ", fieldsToUpdate));

            // Update the issue using RedmineService
            var updatedIssue = await _redmineService.UpdateIssueAsync(
                issueId,
                null, // subject - keep existing
                statusIdOrName,
                assigneeIdOrUsername,
                doneRatio,
                cancellationToken);

            // Show success message with what was updated
            var updateSummary = string.Join(", ", fieldsToUpdate.Select(f =>
                f switch
                {
                    "status" => $"status → {updateDetails.GetValueOrDefault("status", "unknown")}",
                    "assignee" => $"assignee → {updateDetails.GetValueOrDefault("assignee", "unknown")}",
                    "progress" => $"progress → {updateDetails.GetValueOrDefault("progress", "unknown")}",
                    _ => f
                }));

            var profile = await _configService.GetActiveProfileAsync();
            if (profile != null)
            {
                var issueUrl = $"{profile.Url.TrimEnd('/')}/issues/{updatedIssue.Id}";
                AnsiConsole.MarkupLine($"[green]✓[/] Issue #{updatedIssue.Id} updated ({updateSummary})");
                AnsiConsole.MarkupLine($"[dim]View at: {Markup.Escape(issueUrl)}[/]");
            }
            else
            {
                AnsiConsole.MarkupLine($"[green]✓[/] Issue #{updatedIssue.Id} updated ({updateSummary})");
            }

            return 0;
        }
        catch (ValidationException ex)
        {
            _logger.LogError(ex, "Validation error while editing issue {IssueId}", issueId);
            AnsiConsole.MarkupLine($"[red]Error:[/] {ex.Message}");
            return 1;
        }
        catch (RedmineApiException ex)
        {
            _logger.LogError(ex, "API error while editing issue {IssueId}", issueId);

            if (ex.StatusCode == 404)
            {
                AnsiConsole.MarkupLine($"[red]Error:[/] Issue #{issueId} not found.");
            }
            else
            {
                AnsiConsole.MarkupLine($"[red]Error:[/] {ex.Message}");
            }
            return 1;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while editing issue {IssueId}", issueId);
            AnsiConsole.MarkupLine($"[red]Error:[/] {ex.Message}");
            return 1;
        }
    }

    private async Task<int> EditInteractiveAsync(int issueId, CancellationToken cancellationToken)
    {
        try
        {
            AnsiConsole.MarkupLine($"[bold cyan]Edit Issue #{issueId}[/]");
            AnsiConsole.WriteLine();

            // Fetch current issue details
            var currentIssue = await _redmineService.GetIssueAsync(issueId, false, cancellationToken);

            // Show current values
            AnsiConsole.MarkupLine("[dim]Current values:[/]");
            AnsiConsole.MarkupLine($"  Status: {currentIssue.Status?.Name ?? "None"}");
            AnsiConsole.MarkupLine($"  Assignee: {currentIssue.AssignedTo?.DisplayName ?? "None"}");
            AnsiConsole.MarkupLine($"  Progress: {currentIssue.DoneRatio ?? 0}%");
            AnsiConsole.WriteLine();

            // Interactive field selection
            var choices = new List<string> { "Status", "Assignee", "Progress", "Done (save changes)", "Cancel" };
            var updateIssue = new Issue();
            updateIssue.Subject = currentIssue.Subject; // Always include subject
            bool hasChanges = false;

            while (true)
            {
                var choice = AnsiConsole.Prompt(
                    new SelectionPrompt<string>()
                        .Title("What would you like to edit?")
                        .AddChoices(choices));

                if (choice == "Cancel")
                {
                    AnsiConsole.MarkupLine("[yellow]Edit cancelled[/]");
                    return 0;
                }

                if (choice == "Done (save changes)")
                {
                    if (!hasChanges)
                    {
                        AnsiConsole.MarkupLine("[yellow]No changes to save[/]");
                        return 0;
                    }
                    break;
                }

                switch (choice)
                {
                    case "Status":
                        var statuses = await _redmineService.GetIssueStatusesAsync(cancellationToken);
                        var selectedStatus = AnsiConsole.Prompt(
                            new SelectionPrompt<IssueStatus>()
                                .Title("Select new status:")
                                .UseConverter(s => s.Name)
                                .AddChoices(statuses));
                        updateIssue.Status = selectedStatus;
                        hasChanges = true;
                        AnsiConsole.MarkupLine($"[green]Status will be changed to: {selectedStatus.Name}[/]");
                        break;

                    case "Assignee":
                        var assignToMe = AnsiConsole.Confirm("Assign to yourself?", false);
                        if (assignToMe)
                        {
                            var currentUser = await _redmineService.GetCurrentUserAsync(cancellationToken);
                            updateIssue.AssignedTo = currentUser;
                            hasChanges = true;
                            AnsiConsole.MarkupLine($"[green]Will be assigned to: {currentUser.DisplayName}[/]");
                        }
                        else
                        {
                            var users = await _redmineService.GetUsersAsync(null, cancellationToken);
                            var selectedUser = AnsiConsole.Prompt(
                                new SelectionPrompt<User>()
                                    .Title("Select assignee:")
                                    .UseConverter(u => u.DisplayName)
                                    .AddChoices(users));
                            updateIssue.AssignedTo = selectedUser;
                            hasChanges = true;
                            AnsiConsole.MarkupLine($"[green]Will be assigned to: {selectedUser.DisplayName}[/]");
                        }
                        break;

                    case "Progress":
                        var newProgress = AnsiConsole.Prompt(
                            new TextPrompt<int>("Enter progress percentage (0-100):")
                                .DefaultValue(currentIssue.DoneRatio ?? 0)
                                .Validate(value =>
                                {
                                    if (value < 0 || value > 100)
                                        return ValidationResult.Error("Progress must be between 0 and 100");
                                    return ValidationResult.Success();
                                }));
                        updateIssue.DoneRatio = newProgress;
                        hasChanges = true;
                        AnsiConsole.MarkupLine($"[green]Progress will be set to: {newProgress}%[/]");
                        break;
                }
            }

            // Update the issue using RedmineService
            // Extract values from updateIssue
            string? statusIdOrName = updateIssue.Status?.Id.ToString();
            string? assigneeIdOrUsername = updateIssue.AssignedTo?.Id.ToString();

            var updatedIssue = await _redmineService.UpdateIssueAsync(
                issueId,
                null, // subject - keep existing
                statusIdOrName,
                assigneeIdOrUsername,
                updateIssue.DoneRatio,
                cancellationToken);

            // Show success message
            var profile = await _configService.GetActiveProfileAsync();
            if (profile != null)
            {
                var issueUrl = $"{profile.Url.TrimEnd('/')}/issues/{updatedIssue.Id}";
                AnsiConsole.MarkupLine($"[green]✓[/] Issue #{updatedIssue.Id} updated: {Markup.Escape(updatedIssue.Subject)}");
                AnsiConsole.MarkupLine($"[dim]View at: {Markup.Escape(issueUrl)}[/]");
            }
            else
            {
                AnsiConsole.MarkupLine($"[green]✓[/] Issue #{updatedIssue.Id} updated: {Markup.Escape(updatedIssue.Subject)}");
            }

            return 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during interactive issue edit");
            AnsiConsole.MarkupLine($"[red]Error:[/] {ex.Message}");
            return 1;
        }
    }

    public async Task<int> CommentAsync(int issueId, string? message, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Adding comment to issue {IssueId} - Message provided: {HasMessage}", issueId, !string.IsNullOrEmpty(message));

            string commentText;

            // If message is provided via command line, use it directly
            if (message != null)
            {
                // Check if the provided message is empty or whitespace
                if (string.IsNullOrWhiteSpace(message))
                {
                    AnsiConsole.MarkupLine("[yellow]Comment cancelled: No text entered[/]");
                    return 1;
                }
                commentText = message.Trim();
            }
            else
            {
                // Open editor to get comment text
                commentText = await OpenEditorForCommentAsync();
                if (string.IsNullOrWhiteSpace(commentText))
                {
                    AnsiConsole.MarkupLine("[yellow]Comment cancelled: No text entered[/]");
                    return 1;
                }
            }

            // Final validation to ensure comment is not empty
            if (string.IsNullOrEmpty(commentText))
            {
                AnsiConsole.MarkupLine("[red]Error:[/] Comment cannot be empty");
                return 1;
            }

            // Add comment via API
            await _redmineService.AddCommentAsync(issueId, commentText, cancellationToken);

            // Show success message with issue URL
            await ShowSuccessMessageWithUrlAsync(issueId, "Comment added");

            return 0;
        }
        catch (RedmineApiException ex)
        {
            _logger.LogError(ex, "API error while adding comment to issue {IssueId}", issueId);

            if (ex.StatusCode == 404)
            {
                AnsiConsole.MarkupLine($"[red]Error:[/] Issue #{issueId} not found.");
            }
            else
            {
                AnsiConsole.MarkupLine($"[red]Error:[/] {ex.Message}");
            }
            return 1;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while adding comment to issue {IssueId}", issueId);
            AnsiConsole.MarkupLine($"[red]Error:[/] {ex.Message}");
            return 1;
        }
    }

    private async Task<string> OpenEditorForCommentAsync()
    {
        var tempFilePath = Path.GetTempFileName();
        try
        {
            var editor = GetDefaultEditor();
            await WriteCommentTemplateAsync(tempFilePath);
            await LaunchEditorAsync(editor, tempFilePath);
            var content = await ReadAndParseCommentAsync(tempFilePath);
            return content;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to open editor, falling back to console input");
            return await GetCommentFromConsoleAsync();
        }
        finally
        {
            DeleteTempFile(tempFilePath);
        }
    }

    private static string GetDefaultEditor()
    {
        var editor = Environment.GetEnvironmentVariable("EDITOR");
        if (!string.IsNullOrEmpty(editor))
        {
            return editor;
        }

        // Default editors based on platform
        return RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "notepad" : "nano";
    }

    private static async Task WriteCommentTemplateAsync(string filePath)
    {
        const string template = "# Enter your comment above this line\n# Lines starting with # are ignored\n";
        await File.WriteAllTextAsync(filePath, template);
    }

    private static async Task LaunchEditorAsync(string editor, string filePath)
    {
        var processInfo = new ProcessStartInfo
        {
            FileName = editor,
            Arguments = filePath,
            UseShellExecute = false
        };

        var process = Process.Start(processInfo);
        if (process != null)
        {
            await process.WaitForExitAsync();
        }
    }

    private static async Task<string> ReadAndParseCommentAsync(string filePath)
    {
        var content = await File.ReadAllTextAsync(filePath);

        // Remove comment lines and instruction lines
        var lines = content.Split('\n')
            .Where(line => !line.TrimStart().StartsWith("#"))
            .Select(line => line.TrimEnd('\r', '\n'));

        return string.Join('\n', lines).Trim();
    }

    private static async Task<string> GetCommentFromConsoleAsync()
    {
        AnsiConsole.MarkupLine("[yellow]Could not open editor. Please enter comment text:[/]");
        var input = AnsiConsole.Prompt(
            new TextPrompt<string>("Comment:")
                .AllowEmpty());
        return await Task.FromResult(input);
    }

    private static void DeleteTempFile(string filePath)
    {
        try
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }
        catch
        {
            // Ignore cleanup errors
        }
    }

    private async Task ShowSuccessMessageWithUrlAsync(int issueId, string action)
    {
        var profile = await _configService.GetActiveProfileAsync();
        if (profile != null)
        {
            var issueUrl = $"{profile.Url.TrimEnd('/')}/issues/{issueId}";
            AnsiConsole.MarkupLine($"[green]✓[/] {action} to issue #{issueId}");
            AnsiConsole.MarkupLine($"[dim]View at: {Markup.Escape(issueUrl)}[/]");
        }
        else
        {
            AnsiConsole.MarkupLine($"[green]✓[/] {action} to issue #{issueId}");
        }
    }

    public async Task<int> ListAttachmentsAsync(int issueId, bool json, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Listing attachments for issue {IssueId}", issueId);

            var issue = await _redmineService.GetIssueAsync(issueId, false, cancellationToken);

            if (issue.Attachments == null || issue.Attachments.Count == 0)
            {
                AnsiConsole.MarkupLine($"[yellow]No attachments found for issue #{issueId}[/]");
                return 0;
            }

            if (json)
            {
                _jsonFormatter.FormatObject(issue.Attachments);
            }
            else
            {
                _tableFormatter.FormatAttachments(issue.Attachments);
            }

            return 0;
        }
        catch (RedmineApiException ex)
        {
            _logger.LogError(ex, "Failed to list attachments for issue {IssueId}", issueId);
            AnsiConsole.MarkupLine($"[red]Error: {ex.Message}[/]");
            return 1;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error listing attachments for issue {IssueId}", issueId);
            AnsiConsole.MarkupLine("[red]Error: An unexpected error occurred[/]");
            return 1;
        }
    }

    public async Task<int> DownloadAttachmentsAsync(int issueId, bool all, string? outputDirectory, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Downloading attachments for issue {IssueId}", issueId);

            var issue = await _redmineService.GetIssueAsync(issueId, false, cancellationToken);

            if (issue.Attachments == null || issue.Attachments.Count == 0)
            {
                AnsiConsole.MarkupLine($"[yellow]No attachments found for issue #{issueId}[/]");
                return 0;
            }

            List<Attachment> attachmentsToDownload;

            if (all)
            {
                attachmentsToDownload = issue.Attachments;
            }
            else
            {
                // Interactive selection using MultiSelectionPrompt
                attachmentsToDownload = AnsiConsole.Prompt(
                    new MultiSelectionPrompt<Attachment>()
                        .Title("Select attachments to download:")
                        .NotRequired()
                        .UseConverter(a => $"{a.Filename} ({FormatFileSize(a.Filesize)})")
                        .AddChoices(issue.Attachments));

                if (attachmentsToDownload.Count == 0)
                {
                    AnsiConsole.MarkupLine("[yellow]No attachments selected[/]");
                    return 0;
                }
            }

            // Set output directory
            outputDirectory ??= Directory.GetCurrentDirectory();
            if (!Directory.Exists(outputDirectory))
            {
                Directory.CreateDirectory(outputDirectory);
            }

            var downloadedCount = 0;
            var errors = new List<string>();
            var errorLock = new object();

            // Track used filenames to avoid conflicts
            var usedFilenames = new HashSet<string>();
            var filenameLock = new object();

            // Download attachments with progress
            await AnsiConsole.Progress()
                .Columns(new ProgressColumn[]
                {
                    new TaskDescriptionColumn(),
                    new ProgressBarColumn(),
                    new PercentageColumn(),
                    new SpinnerColumn()
                })
                .StartAsync(async ctx =>
                {
                    var downloadTasks = new List<Task>();

                    foreach (var attachment in attachmentsToDownload)
                    {
                        var task = ctx.AddTask($"[green]Downloading {attachment.Filename}[/]");

                        downloadTasks.Add(Task.Run(async () =>
                        {
                            try
                            {
                                using var stream = await _redmineService.DownloadAttachmentAsync(attachment.Id, cancellationToken);

                                string fileName;
                                string filePath;
                                FileStream? fileStream = null;

                                // Generate unique filename and create file (thread-safe)
                                lock (filenameLock)
                                {
                                    fileName = attachment.Filename;
                                    filePath = Path.Combine(outputDirectory, fileName);

                                    var counter = 1;
                                    while (File.Exists(filePath) || usedFilenames.Contains(filePath))
                                    {
                                        var fileNameWithoutExt = Path.GetFileNameWithoutExtension(attachment.Filename);
                                        var extension = Path.GetExtension(attachment.Filename);
                                        fileName = $"{fileNameWithoutExt}_{counter}{extension}";
                                        filePath = Path.Combine(outputDirectory, fileName);
                                        counter++;
                                    }

                                    usedFilenames.Add(filePath);
                                    // Create file inside the lock to avoid race conditions
                                    fileStream = File.Create(filePath);
                                }

                                try
                                {
                                    using (fileStream)
                                    {
                                        await stream.CopyToAsync(fileStream);
                                    }
                                }
                                catch
                                {
                                    // Clean up on error
                                    lock (filenameLock)
                                    {
                                        usedFilenames.Remove(filePath);
                                    }
                                    throw;
                                }

                                task.Increment(100);
                                Interlocked.Increment(ref downloadedCount);

                                AnsiConsole.MarkupLine($"[green]✓[/] Downloaded {Markup.Escape(attachment.Filename)} as {Markup.Escape(fileName)}");
                            }
                            catch (Exception ex)
                            {
                                task.StopTask();
                                lock (errorLock)
                                {
                                    errors.Add($"{attachment.Filename}: {ex.Message}");
                                }
                                _logger.LogError(ex, "Failed to download attachment {AttachmentId}", attachment.Id);
                            }
                        }));
                    }

                    await Task.WhenAll(downloadTasks);
                });

            if (downloadedCount > 0)
            {
                AnsiConsole.MarkupLine($"[green]Downloaded {downloadedCount} attachment{(downloadedCount > 1 ? "s" : "")}[/]");
            }

            if (errors.Count > 0)
            {
                AnsiConsole.MarkupLine("[red]Failed to download:[/]");
                foreach (var error in errors)
                {
                    AnsiConsole.MarkupLine($"  [red]✗[/] {Markup.Escape(error)}");
                }
                return 1;
            }

            return 0;
        }
        catch (RedmineApiException ex)
        {
            _logger.LogError(ex, "Failed to download attachments for issue {IssueId}", issueId);
            AnsiConsole.MarkupLine($"[red]Error: {ex.Message}[/]");
            return 1;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error downloading attachments for issue {IssueId}", issueId);
            AnsiConsole.MarkupLine("[red]Error: An unexpected error occurred[/]");
            return 1;
        }
    }

    private static string FormatFileSize(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len = len / 1024;
        }
        return $"{len:0.#} {sizes[order]}";
    }

    private static bool IsValidSortParameter(string sort)
    {
        var validFields = new HashSet<string>
        {
            "id", "subject", "status", "priority", "author", "assigned_to",
            "updated_on", "created_on", "start_date", "due_date", "done_ratio",
            "category", "fixed_version"
        };

        // Split by comma for multiple sort fields
        var sortFields = sort.Split(',');

        foreach (var sortField in sortFields)
        {
            // Split by colon to get field and direction
            var parts = sortField.Trim().Split(':');

            if (parts.Length == 0 || parts.Length > 2)
                return false;

            var field = parts[0].Trim();

            // Check if field is valid
            if (!validFields.Contains(field))
                return false;

            // If direction is specified, check if it's valid
            if (parts.Length == 2)
            {
                var direction = parts[1].Trim().ToLower();
                if (direction != "asc" && direction != "desc")
                    return false;
            }
        }

        return true;
    }

    public async Task<int> CloseAsync(
        int[] issueIds,
        string? message,
        string? status,
        int? doneRatio,
        bool json,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Closing issues {IssueIds} - Message: {HasMessage}, Status: {Status}, DoneRatio: {DoneRatio}",
                issueIds, !string.IsNullOrEmpty(message), status, doneRatio);

            if (issueIds.Length == 0)
            {
                AnsiConsole.MarkupLine("[red]Error:[/] No issue IDs specified");
                return 1;
            }

            // If no done ratio specified, default to 100
            doneRatio ??= 100;

            // Validate done ratio
            if (doneRatio < 0 || doneRatio > 100)
            {
                AnsiConsole.MarkupLine("[red]Error:[/] Done ratio must be between 0 and 100");
                return 1;
            }

            var closedIssues = new List<Issue>();
            var errors = new List<string>();

            // Get available statuses to find default close status if needed
            List<IssueStatus>? statuses = null;
            string? closeStatusId = status;

            if (string.IsNullOrEmpty(status))
            {
                // Get statuses and find the first closed status
                statuses = await _redmineService.GetIssueStatusesAsync(cancellationToken);
                var defaultCloseStatus = statuses.FirstOrDefault(s => s.IsClosed == true);

                if (defaultCloseStatus == null)
                {
                    AnsiConsole.MarkupLine("[red]Error:[/] No closed status found in the system");
                    return 1;
                }

                closeStatusId = defaultCloseStatus.Id.ToString();
                _logger.LogDebug("Using default close status: {StatusName} (ID: {StatusId})", defaultCloseStatus.Name, defaultCloseStatus.Id);
            }
            else
            {
                // Validate the specified status
                statuses = await _redmineService.GetIssueStatusesAsync(cancellationToken);
                var (resolvedStatusId, resolvedStatus) = await ResolveStatusForEditAsync(status, cancellationToken);

                if (resolvedStatus != null && resolvedStatus.IsClosed != true)
                {
                    AnsiConsole.MarkupLine($"[yellow]Warning:[/] Status '{resolvedStatus.Name}' is not a closed status, but proceeding anyway");
                }

                closeStatusId = resolvedStatusId;
            }

            // Process each issue
            foreach (var issueId in issueIds)
            {
                try
                {
                    // Get current issue to check if already closed
                    var currentIssue = await _redmineService.GetIssueAsync(issueId, false, cancellationToken);

                    // Check if already closed
                    if (currentIssue.Status?.IsClosed == true)
                    {
                        AnsiConsole.MarkupLine($"[yellow]Warning:[/] Issue #{issueId} is already closed (status: {currentIssue.Status.Name})");
                        closedIssues.Add(currentIssue);
                        continue;
                    }

                    // Update the issue
                    var updatedIssue = await _redmineService.UpdateIssueAsync(
                        issueId,
                        null, // keep existing subject
                        closeStatusId,
                        null, // keep existing assignee
                        doneRatio,
                        cancellationToken);

                    // Add comment if specified
                    if (!string.IsNullOrEmpty(message))
                    {
                        await _redmineService.AddCommentAsync(issueId, message, cancellationToken);
                    }

                    closedIssues.Add(updatedIssue);

                    if (!json)
                    {
                        var profile = await _configService.GetActiveProfileAsync();
                        if (profile != null)
                        {
                            var issueUrl = $"{profile.Url.TrimEnd('/')}/issues/{updatedIssue.Id}";
                            AnsiConsole.MarkupLine($"[green]✓[/] Issue #{updatedIssue.Id} closed: {Markup.Escape(updatedIssue.Subject)}");
                            AnsiConsole.MarkupLine($"[dim]View at: {Markup.Escape(issueUrl)}[/]");
                        }
                        else
                        {
                            AnsiConsole.MarkupLine($"[green]✓[/] Issue #{updatedIssue.Id} closed: {Markup.Escape(updatedIssue.Subject)}");
                        }
                    }
                }
                catch (RedmineApiException ex) when (ex.StatusCode == 404)
                {
                    errors.Add($"Issue #{issueId}: Not found");
                    _logger.LogError(ex, "Issue {IssueId} not found", issueId);
                }
                catch (Exception ex)
                {
                    errors.Add($"Issue #{issueId}: {ex.Message}");
                    _logger.LogError(ex, "Failed to close issue {IssueId}", issueId);
                }
            }

            // Output results
            if (json)
            {
                var result = new
                {
                    closed = closedIssues.Select(i => new
                    {
                        i.Id,
                        i.Subject,
                        Status = i.Status?.Name,
                        DoneRatio = i.DoneRatio
                    }),
                    errors = errors.Select(e => new { error = e })
                };
                _jsonFormatter.FormatObject(result);
            }
            else if (errors.Count > 0)
            {
                AnsiConsole.WriteLine();
                AnsiConsole.MarkupLine("[red]Failed to close some issues:[/]");
                foreach (var error in errors)
                {
                    AnsiConsole.MarkupLine($"  [red]✗[/] {Markup.Escape(error)}");
                }
            }

            // Return non-zero if any errors occurred
            return errors.Count > 0 ? 1 : 0;
        }
        catch (ValidationException ex)
        {
            _logger.LogError(ex, "Validation error while closing issues");
            AnsiConsole.MarkupLine($"[red]Error:[/] {ex.Message}");
            return 1;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while closing issues");
            AnsiConsole.MarkupLine($"[red]Error:[/] {ex.Message}");
            return 1;
        }
    }
}
