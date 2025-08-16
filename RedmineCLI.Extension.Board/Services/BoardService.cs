using System.Net.Http;
using System.Text.Json;

using Microsoft.Extensions.Logging;

using RedmineCLI.Extension.Board.Models;

using Spectre.Console;

namespace RedmineCLI.Extension.Board.Services;

/// <summary>
/// ボード関連操作のためのサービス実装
/// </summary>
public class BoardService : IBoardService
{
    private readonly ILogger<BoardService> _logger;
    private readonly IAuthenticationService _authenticationService;
    private readonly IHtmlParsingService _htmlParsingService;

    public BoardService(
        ILogger<BoardService> logger,
        IAuthenticationService authenticationService,
        IHtmlParsingService htmlParsingService)
    {
        _logger = logger;
        _authenticationService = authenticationService;
        _htmlParsingService = htmlParsingService;
    }

    public async Task ListBoardsAsync(string? projectFilter, string? urlOverride)
    {
        _logger.LogInformation("Starting board listing");
        // _logger.LogDebug("Project filter: {Filter}", projectFilter ?? "(none)");

        // Get authentication from OS keychain
        var (redmineUrl, sessionCookie) = await _authenticationService.GetAuthenticationAsync(urlOverride);

        if (string.IsNullOrEmpty(sessionCookie))
        {
            _logger.LogError("No session cookie available");
            AnsiConsole.MarkupLine("[red]Error: Could not create session.[/]");
            AnsiConsole.MarkupLine("Please ensure you have saved password credentials with [cyan]redmine auth login --save-password[/].");
            Environment.Exit(1);
            return;
        }

        // _logger.LogDebug("Using session cookie for {Url}", redmineUrl);

        using var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Add("User-Agent", "RedmineCLI-Board/1.0");
        httpClient.DefaultRequestHeaders.Add("Cookie", sessionCookie);

        try
        {
            // Project filter is required
            if (string.IsNullOrEmpty(projectFilter))
            {
                _logger.LogError("Project filter is required");
                AnsiConsole.MarkupLine("[red]Error: Project identifier is required.[/]");
                AnsiConsole.MarkupLine("Usage: [cyan]redmine-board list --project <project-identifier>[/]");
                Environment.Exit(1);
                return;
            }

            List<Models.Board> allBoards = new List<Models.Board>();

            // Check board for the specified project
            // _logger.LogDebug("Checking board for specific project: {Project}", projectFilter);

            var boardUrl = $"{redmineUrl}/projects/{projectFilter}/boards";
            // _logger.LogDebug("Checking board URL: {Url}", boardUrl);

            try
            {
                var response = await httpClient.GetAsync(boardUrl);
                // _logger.LogDebug("Response status for {Url}: {Status}", boardUrl, response.StatusCode);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    // _logger.LogDebug("Response content length: {Length} bytes", content.Length);

                    // // デバッグ用：HTMLをファイルに保存
                    // if (_logger.IsEnabled(LogLevel.Debug))
                    // {
                    //     var debugPath = Path.Combine(Path.GetTempPath(), "redmine_boards_debug.html");
                    //     await File.WriteAllTextAsync(debugPath, content);
                    //     _logger.LogDebug("HTML saved to: {Path}", debugPath);
                    // }

                    // Parse boards from HTML
                    var boards = _htmlParsingService.ParseBoardsFromHtml(content, redmineUrl);

                    foreach (var board in boards)
                    {
                        board.ProjectName = projectFilter;
                        board.ProjectId = ParseProjectId(projectFilter);
                    }

                    if (boards.Any())
                    {
                        _logger.LogInformation("Found {Count} boards in project {Project}",
                            boards.Count, projectFilter);
                        allBoards.AddRange(boards);
                    }
                    else
                    {
                        // _logger.LogDebug("No boards found in project {Project}", projectFilter);
                    }
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    // _logger.LogDebug("Boards not available for project {Project}", projectFilter);
                    AnsiConsole.MarkupLine($"[yellow]No boards found for project '[cyan]{projectFilter}[/]'.[/]");
                    AnsiConsole.MarkupLine("[dim]Note: Board functionality requires a board plugin to be installed on the Redmine server.[/]");
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    _logger.LogError("Session expired or invalid");
                    AnsiConsole.MarkupLine("[red]Error: Session expired.[/]");
                    AnsiConsole.MarkupLine("Please run [cyan]redmine auth login --save-password[/] again.");
                    Environment.Exit(1);
                    return;
                }
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Failed to fetch board URL");
                AnsiConsole.MarkupLine($"[red]Error: Failed to connect to Redmine server[/]");
                AnsiConsole.MarkupLine($"[dim]{ex.Message}[/]");
                Environment.Exit(1);
                return;
            }

            // Display results
            if (allBoards.Any())
            {
                AnsiConsole.MarkupLine($"[bold]Found {allBoards.Count} board(s) for project '[cyan]{projectFilter}[/]'[/]");
                AnsiConsole.WriteLine();

                // Create table
                var table = new Table();
                table.AddColumn(new TableColumn("[yellow]ID[/]").Centered());
                table.AddColumn(new TableColumn("[yellow]Forum[/]"));
                table.AddColumn(new TableColumn("[yellow]Project[/]"));
                table.AddColumn(new TableColumn("[yellow]Topics[/]").Centered());
                table.AddColumn(new TableColumn("[yellow]Messages[/]").Centered());

                // Add rows
                foreach (var board in allBoards.DistinctBy(b => b.Id))
                {
                    table.AddRow(
                        board.Id.ToString(),
                        Markup.Escape(board.Name),
                        Markup.Escape(board.ProjectName ?? "N/A"),
                        board.ColumnCount.ToString(),
                        board.CardCount.ToString()
                    );
                }

                // Display table
                AnsiConsole.Write(table);

                // // Save boards to JSON for debugging
                // if (_logger.IsEnabled(LogLevel.Debug))
                // {
                //     var context = new BoardJsonContext();
                //     var json = JsonSerializer.Serialize(allBoards, typeof(List<Models.Board>), context);
                //     _logger.LogDebug("Boards JSON: {Json}", json);
                // }
            }
            else
            {
                AnsiConsole.MarkupLine("[yellow]No boards found.[/]");
                AnsiConsole.WriteLine();
                AnsiConsole.MarkupLine("[dim]Note: Board functionality usually requires a plugin to be installed on the Redmine server.[/]");
                AnsiConsole.MarkupLine("[dim]Common plugins: Redmine Agile, Backlogs, or custom board plugins.[/]");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing boards");
            Console.Error.WriteLine($"Error: {ex.Message}");
            Environment.Exit(1);
        }
    }

    public async Task ListTopicsAsync(string boardIdString, string? projectName, (string SessionCookie, string BaseUrl) auth)
    {
        if (!int.TryParse(boardIdString, out var boardId))
        {
            AnsiConsole.MarkupLine("[red]Invalid board ID.[/]");
            return;
        }

        var boardUrl = $"{auth.BaseUrl}/boards/{boardId}";
        if (!string.IsNullOrEmpty(projectName))
        {
            boardUrl = $"{auth.BaseUrl}/projects/{projectName}/boards/{boardId}";
        }

        using var client = new HttpClient();
        client.DefaultRequestHeaders.Add("Cookie", auth.SessionCookie);

        var response = await client.GetAsync(boardUrl);
        if (!response.IsSuccessStatusCode)
        {
            AnsiConsole.MarkupLine($"[red]Failed to fetch board: {response.StatusCode}[/]");
            return;
        }

        var html = await response.Content.ReadAsStringAsync();
        var topics = _htmlParsingService.ParseTopicsFromHtml(html);

        if (topics.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No topics found.[/]");
            return;
        }

        var table = new Table();
        table.AddColumn("ID");
        table.AddColumn("Title");
        table.AddColumn("Author");
        table.AddColumn("Replies");
        table.AddColumn("Last Reply");
        table.AddColumn("Status");

        foreach (var topic in topics)
        {
            var status = "";
            if (topic.IsSticky) status += "📌 ";
            if (topic.IsLocked) status += "🔒";

            table.AddRow(
                topic.Id.ToString(),
                Markup.Escape(topic.Title),
                Markup.Escape(topic.Author),
                topic.Replies.ToString(),
                topic.LastReply?.ToString("yyyy-MM-dd HH:mm") ?? "-",
                status
            );
        }

        AnsiConsole.Write(table);
    }

    public async Task ViewTopicAsync(string boardIdString, string topicIdString, string? projectName, (string SessionCookie, string BaseUrl) auth)
    {
        if (!int.TryParse(boardIdString, out var boardId))
        {
            AnsiConsole.MarkupLine("[red]Invalid board ID.[/]");
            return;
        }

        if (!int.TryParse(topicIdString, out var topicId))
        {
            AnsiConsole.MarkupLine("[red]Invalid topic ID.[/]");
            return;
        }

        // Build the URL for board topics (always use /boards/{boardId}/topics/{topicId} format)
        var topicUrl = $"{auth.BaseUrl}/boards/{boardId}/topics/{topicId}";

        // _logger.LogDebug("Fetching topic from URL: {Url}", topicUrl);
        // if (!string.IsNullOrEmpty(projectName))
        // {
        //     _logger.LogDebug("Project name '{Project}' was provided but not used in URL", projectName);
        // }

        using var client = new HttpClient();
        client.DefaultRequestHeaders.Add("Cookie", auth.SessionCookie);

        var response = await client.GetAsync(topicUrl);
        if (!response.IsSuccessStatusCode)
        {
            AnsiConsole.MarkupLine($"[red]Failed to fetch topic: {response.StatusCode}[/]");
            return;
        }

        var html = await response.Content.ReadAsStringAsync();
        var topicDetail = _htmlParsingService.ParseTopicDetailFromHtml(html);

        if (topicDetail == null)
        {
            AnsiConsole.MarkupLine("[red]Failed to parse topic details.[/]");
            return;
        }

        // トピックのタイトルを表示
        AnsiConsole.MarkupLine($"[bold]{Markup.Escape(topicDetail.Title)}[/]");
        AnsiConsole.WriteLine();

        // 最初の投稿を表示
        var createdAt = topicDetail.CreatedAt != default ? FormatRelativeTime(topicDetail.CreatedAt) : "unknown";
        AnsiConsole.MarkupLine($"[dim]#{topicDetail.Id} - {Markup.Escape(topicDetail.Author)} - {createdAt}[/]");
        AnsiConsole.MarkupLine($"  {Markup.Escape(topicDetail.Content)}");
        AnsiConsole.WriteLine();

        // 返信を表示
        if (topicDetail.Replies.Count > 0)
        {
            // 返信を時系列順にソート
            var sortedReplies = topicDetail.Replies.OrderBy(r => r.CreatedAt).ToList();
            var newestReply = topicDetail.Replies.OrderByDescending(r => r.CreatedAt).FirstOrDefault();

            foreach (var reply in sortedReplies)
            {
                var replyTime = FormatRelativeTime(reply.CreatedAt);
                var newestLabel = reply == newestReply ? " - Newest post" : "";
                AnsiConsole.MarkupLine($"[dim]#{reply.Id} - {Markup.Escape(reply.Author)} - {replyTime}{newestLabel}[/]");
                AnsiConsole.MarkupLine($"  {Markup.Escape(reply.Content)}");
                AnsiConsole.WriteLine();
            }
        }
    }

    private int? ParseProjectId(string identifier)
    {
        if (int.TryParse(identifier, out var id))
            return id;
        return null;
    }

    private string FormatRelativeTime(DateTime dateTime)
    {
        var timeSpan = DateTime.Now - dateTime;

        if (timeSpan.TotalDays >= 365)
        {
            var years = (int)(timeSpan.TotalDays / 365);
            return $"about {years} year{(years == 1 ? "" : "s")} ago";
        }
        if (timeSpan.TotalDays >= 30)
        {
            var months = (int)(timeSpan.TotalDays / 30);
            return $"about {months} month{(months == 1 ? "" : "s")} ago";
        }
        if (timeSpan.TotalDays >= 1)
        {
            var days = (int)timeSpan.TotalDays;
            return $"about {days} day{(days == 1 ? "" : "s")} ago";
        }
        if (timeSpan.TotalHours >= 1)
        {
            var hours = (int)timeSpan.TotalHours;
            return $"about {hours} hour{(hours == 1 ? "" : "s")} ago";
        }
        if (timeSpan.TotalMinutes >= 1)
        {
            var minutes = (int)timeSpan.TotalMinutes;
            return $"about {minutes} minute{(minutes == 1 ? "" : "s")} ago";
        }

        return "just now";
    }
}
