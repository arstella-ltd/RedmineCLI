using System;
using System.CommandLine;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

using HtmlAgilityPack;

using Microsoft.Extensions.Logging;

using RedmineCLI.Common.Models;
using RedmineCLI.Common.Services;
using RedmineCLI.Extension.Board.Models;

using Spectre.Console;

namespace RedmineCLI.Extension.Board;

/// <summary>
/// RedmineCLI Board Extension - Provides board management functionality with form-based login
/// </summary>
public class Program
{
    private static ILogger<Program>? _logger;
    private static ICredentialStore? _credentialStore;

    public static async Task<int> Main(string[] args)
    {
        // Setup logging with debug output
        using var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder
                .AddConsole()
                .SetMinimumLevel(LogLevel.Debug);
        });
        _logger = loggerFactory.CreateLogger<Program>();
        _credentialStore = CredentialStore.Create();

        _logger.LogDebug("Starting RedmineCLI Board Extension");
        _logger.LogDebug("Arguments: {Args}", string.Join(" ", args));

        var rootCommand = new RootCommand("RedmineCLI Board Extension - Manage Redmine boards");

        // List command (with ls alias) - for listing all boards
        var listCommand = new Command("list", "List all boards (requires 'redmine auth login' first)");
        listCommand.AddAlias("ls");
        var projectOption = new Option<string>("--project", "Filter by project name or ID");
        var urlOption = new Option<string>("--url", "Redmine server URL (optional, uses stored credentials by default)");
        listCommand.Add(projectOption);
        listCommand.Add(urlOption);
        listCommand.SetHandler(async (string? project, string? url) =>
        {
            await ListBoardsAsync(project, url);
        }, projectOption, urlOption);

        rootCommand.AddCommand(listCommand);

        // Board ID as first positional argument
        var boardIdArgument = new Argument<string>("board-id", "Board ID");
        
        // Create a dynamic command for board ID (e.g., "21")
        // This allows: redmine-board 21 topic list
        var topicCommand = new Command("topic", "Topic operations");
        
        // Topic list subcommand
        var topicListCommand = new Command("list", "List topics in the board");
        topicListCommand.AddAlias("ls");
        var topicListProjectOption = new Option<string>("--project", "Project name or ID");
        topicListCommand.Add(topicListProjectOption);
        
        // Topic view with topic ID
        var topicIdArgument = new Argument<string>("topic-id", "Topic ID");
        var topicViewProjectOption = new Option<string>("--project", "Project name or ID");
        
        // We need to handle numbered commands dynamically
        // Check if first arg is a number (board ID)
        if (args.Length > 0 && int.TryParse(args[0], out _))
        {
            // Create a command for this specific board ID
            var boardCommand = new Command(args[0], $"Operations for board {args[0]}");
            boardCommand.IsHidden = true; // Hide from help
            
            // Re-create topic command structure under this board command
            var boardTopicCommand = new Command("topic", "Topic operations");
            
            var boardTopicListCommand = new Command("list", "List topics in the board");
            boardTopicListCommand.AddAlias("ls");
            var boardTopicListProjectOption = new Option<string>("--project", "Project name or ID");
            boardTopicListCommand.Add(boardTopicListProjectOption);
            boardTopicListCommand.SetHandler(async (string? project) =>
            {
                var (url, sessionCookie) = await GetAuthenticationAsync(null);
                if (!string.IsNullOrEmpty(sessionCookie))
                {
                    await ListTopicsAsync(args[0], project, (sessionCookie, url));
                }
            }, boardTopicListProjectOption);
            
            // Topic view command (when topic ID is provided)
            var boardTopicIdArgument = new Argument<string>("topic-id", "Topic ID");
            boardTopicCommand.Add(boardTopicIdArgument);
            var boardTopicViewProjectOption = new Option<string>("--project", "Project name or ID");
            boardTopicCommand.Add(boardTopicViewProjectOption);
            boardTopicCommand.SetHandler(async (string topicId, string? project) =>
            {
                var (url, sessionCookie) = await GetAuthenticationAsync(null);
                if (!string.IsNullOrEmpty(sessionCookie))
                {
                    await ViewTopicAsync(args[0], topicId, project, (sessionCookie, url));
                }
            }, boardTopicIdArgument, boardTopicViewProjectOption);
            
            boardTopicCommand.AddCommand(boardTopicListCommand);
            boardCommand.AddCommand(boardTopicCommand);
            rootCommand.AddCommand(boardCommand);
        }

        // Info command
        var infoCommand = new Command("info", "Display extension and environment information");
        infoCommand.SetHandler(() =>
        {
            DisplayInfo();
        });

        rootCommand.AddCommand(infoCommand);

        var result = await rootCommand.InvokeAsync(args);
        _logger?.LogDebug("Extension exiting with code: {ExitCode}", result);
        return result;
    }

    private static async Task<(string url, string? sessionCookie)> GetAuthenticationAsync(string? urlOverride)
    {
        _logger?.LogDebug("Getting authentication information");

        // Determine Redmine URL
        string? redmineUrl = urlOverride;

        if (string.IsNullOrEmpty(redmineUrl))
        {
            redmineUrl = Environment.GetEnvironmentVariable("REDMINE_URL");
        }


        if (string.IsNullOrEmpty(redmineUrl))
        {
            // Try to get from stored config
            var configDir = Environment.GetEnvironmentVariable("REDMINE_CONFIG_DIR") ??
                           Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "redmine");
            var configFile = Path.Combine(configDir, "config.yml");

            if (File.Exists(configFile))
            {
                try
                {
                    var configContent = await File.ReadAllTextAsync(configFile);
                    // Simple YAML parsing for URL
                    var urlMatch = Regex.Match(configContent, @"url:\s*(.+)");
                    if (urlMatch.Success)
                    {
                        redmineUrl = urlMatch.Groups[1].Value.Trim();
                        // Remove quotes if present (YAML string values)
                        redmineUrl = redmineUrl.Trim('"', '\'');
                        _logger?.LogDebug("Found URL in config file: {Url}", redmineUrl);
                    }
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex, "Could not read config file");
                }
            }
        }

        if (string.IsNullOrEmpty(redmineUrl))
        {
            _logger?.LogError("No Redmine URL configured");
            AnsiConsole.MarkupLine("[red]Error: No Redmine URL found.[/]");
            AnsiConsole.MarkupLine("Please specify [cyan]--url[/] or run [cyan]redmine auth login[/] first.");
            Environment.Exit(1);
        }

        // Normalize URL - ensure it has a scheme
        redmineUrl = redmineUrl.TrimEnd('/');
        if (!redmineUrl.StartsWith("http://") && !redmineUrl.StartsWith("https://"))
        {
            // Default to https if no scheme is provided
            redmineUrl = $"https://{redmineUrl}";
            _logger?.LogDebug("Added https:// scheme to URL");
        }

        _logger?.LogDebug("Using Redmine URL: {Url}", redmineUrl);

        // Get credentials from OS keychain
        try
        {
            var credential = await _credentialStore!.GetCredentialAsync(redmineUrl);
            if (credential == null)
            {
                _logger?.LogError("No credentials found in keychain for {Url}", redmineUrl);
                AnsiConsole.MarkupLine($"[red]Error: No credentials found for {redmineUrl}.[/]");
                AnsiConsole.MarkupLine("Please run [cyan]redmine auth login --save-password[/] first.");
                Environment.Exit(1);
            }

            _logger?.LogDebug("Found credentials for {Url}", redmineUrl);

            // Try to get or create session
            var sessionCookie = await AuthenticationHelper.CreateSessionFromCredentialsAsync(
                redmineUrl,
                credential,
                _logger);

            if (!string.IsNullOrEmpty(sessionCookie))
            {
                _logger?.LogInformation("Successfully authenticated with Redmine");

                // Update stored credential with new session if it changed
                if (sessionCookie != credential.SessionCookie)
                {
                    credential.SessionCookie = sessionCookie;
                    credential.SessionExpiry = DateTime.UtcNow.AddHours(24);
                    await _credentialStore.SaveCredentialAsync(redmineUrl, credential);
                    _logger?.LogDebug("Updated session cookie in keychain");
                }

                return (redmineUrl, sessionCookie);
            }

            // If we have API key but no session, we can still try with API key
            if (!string.IsNullOrEmpty(credential.ApiKey))
            {
                _logger?.LogWarning("No session cookie available, will try with API key");
                return (redmineUrl, null);
            }

            _logger?.LogError("Failed to create session from stored credentials");
            AnsiConsole.MarkupLine("[red]Error: Failed to authenticate.[/]");
            AnsiConsole.MarkupLine("Please run [cyan]redmine auth login --save-password[/] again.");
            Environment.Exit(1);
            return (redmineUrl, null);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error accessing keychain");
            AnsiConsole.MarkupLine($"[red]Error accessing keychain: {ex.Message}[/]");
            AnsiConsole.MarkupLine("Please run [cyan]redmine auth login --save-password[/] first.");
            Environment.Exit(1);
            return (redmineUrl, null);
        }
    }

    private static async Task ListBoardsAsync(string? projectFilter, string? urlOverride)
    {
        _logger?.LogInformation("Starting board listing");
        _logger?.LogDebug("Project filter: {Filter}", projectFilter ?? "(none)");

        // Get authentication from OS keychain
        var (redmineUrl, sessionCookie) = await GetAuthenticationAsync(urlOverride);

        if (string.IsNullOrEmpty(sessionCookie))
        {
            _logger?.LogError("No session cookie available");
            AnsiConsole.MarkupLine("[red]Error: Could not create session.[/]");
            AnsiConsole.MarkupLine("Please ensure you have saved password credentials with [cyan]redmine auth login --save-password[/].");
            Environment.Exit(1);
            return;
        }

        _logger?.LogDebug("Using session cookie for {Url}", redmineUrl);

        using var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Add("User-Agent", "RedmineCLI-Board/1.0");
        httpClient.DefaultRequestHeaders.Add("Cookie", sessionCookie);

        try
        {
            // Project filter is required
            if (string.IsNullOrEmpty(projectFilter))
            {
                _logger?.LogError("Project filter is required");
                AnsiConsole.MarkupLine("[red]Error: Project identifier is required.[/]");
                AnsiConsole.MarkupLine("Usage: [cyan]redmine-board list --project <project-identifier>[/]");
                Environment.Exit(1);
                return;
            }

            List<Models.Board> allBoards = new List<Models.Board>();

            // Check board for the specified project
            _logger?.LogDebug("Checking board for specific project: {Project}", projectFilter);

            var boardUrl = $"{redmineUrl}/projects/{projectFilter}/boards";
            _logger?.LogDebug("Checking board URL: {Url}", boardUrl);

            try
            {
                var response = await httpClient.GetAsync(boardUrl);
                _logger?.LogDebug("Response status for {Url}: {Status}", boardUrl, response.StatusCode);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    _logger?.LogDebug("Response content length: {Length} bytes", content.Length);

                    // デバッグ用：HTMLをファイルに保存
                    if (_logger?.IsEnabled(LogLevel.Debug) == true)
                    {
                        var debugPath = Path.Combine(Path.GetTempPath(), "redmine_boards_debug.html");
                        await File.WriteAllTextAsync(debugPath, content);
                        _logger?.LogDebug("HTML saved to: {Path}", debugPath);
                    }

                    // Parse boards from HTML
                    var boards = ParseBoardsFromHtml(content, redmineUrl);

                    foreach (var board in boards)
                    {
                        board.ProjectName = projectFilter;
                        board.ProjectId = ParseProjectId(projectFilter);
                    }

                    if (boards.Any())
                    {
                        _logger?.LogInformation("Found {Count} boards in project {Project}",
                            boards.Count, projectFilter);
                        allBoards.AddRange(boards);
                    }
                    else
                    {
                        _logger?.LogDebug("No boards found in project {Project}", projectFilter);
                    }
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    _logger?.LogDebug("Boards not available for project {Project}", projectFilter);
                    AnsiConsole.MarkupLine($"[yellow]No boards found for project '[cyan]{projectFilter}[/]'.[/]");
                    AnsiConsole.MarkupLine("[dim]Note: Board functionality requires a board plugin to be installed on the Redmine server.[/]");
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    _logger?.LogError("Session expired or invalid");
                    AnsiConsole.MarkupLine("[red]Error: Session expired.[/]");
                    AnsiConsole.MarkupLine("Please run [cyan]redmine auth login --save-password[/] again.");
                    Environment.Exit(1);
                    return;
                }
            }
            catch (HttpRequestException ex)
            {
                _logger?.LogError(ex, "Failed to fetch board URL");
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

                // Save boards to JSON for debugging
                if (_logger?.IsEnabled(LogLevel.Debug) == true)
                {
                    var context = new BoardJsonContext();
                    var json = JsonSerializer.Serialize(allBoards, typeof(List<Models.Board>), context);
                    _logger.LogDebug("Boards JSON: {Json}", json);
                }
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
            _logger?.LogError(ex, "Error listing boards");
            Console.Error.WriteLine($"Error: {ex.Message}");
            Environment.Exit(1);
        }
    }

    private static List<Models.Board> ParseBoardsFromHtml(string html, string baseUrl)
    {
        var boards = new List<Models.Board>();
        _logger?.LogDebug("Parsing HTML for boards (content length: {Length})", html.Length);

        try
        {
            var doc = new HtmlAgilityPack.HtmlDocument();
            doc.LoadHtml(html);

            // テーブル形式のボード一覧を探す（<tr class="board">タグを利用）
            var boardRows = doc.DocumentNode.SelectNodes("//tr[@class='board']");
            if (boardRows == null)
            {
                _logger?.LogDebug("No board rows found in HTML");
                return boards;
            }

            _logger?.LogDebug("Found {Count} board table rows", boardRows.Count);

            foreach (var row in boardRows)
            {
                var board = ParseBoardFromRow(row, baseUrl);
                if (board != null)
                {
                    boards.Add(board);
                    _logger?.LogDebug("Found board: {Name} (ID: {Id}, Topics: {Topics}, Messages: {Messages})",
                        board.Name, board.Id, board.ColumnCount, board.CardCount);
                }
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error parsing boards from HTML");
        }

        _logger?.LogDebug("Parsed {Count} boards from HTML", boards.Count);
        return boards;
    }

    private static Models.Board? ParseBoardFromRow(HtmlAgilityPack.HtmlNode row, string baseUrl)
    {
        // ボードへのリンクを取得
        var linkNode = row.SelectSingleNode(".//a[contains(@href, '/boards/')]");
        if (linkNode == null)
        {
            _logger?.LogDebug("No board link found in row");
            return null;
        }

        var href = linkNode.GetAttributeValue("href", "");
        var boardId = ExtractBoardId(href);
        if (boardId == null)
        {
            _logger?.LogDebug("Could not extract board ID from href: {Href}", href);
            return null;
        }

        // ボード名を取得
        var boardName = ExtractBoardName(linkNode, boardId.Value);

        // トピック数とメッセージ数を取得
        var topicCount = ExtractCount(row, "topic-count");
        var messageCount = ExtractCount(row, "message-count");

        return new Models.Board
        {
            Id = boardId.Value,
            Name = System.Net.WebUtility.HtmlDecode(boardName),
            Url = $"{baseUrl}{href}",
            ColumnCount = topicCount,
            CardCount = messageCount
        };
    }

    private static int? ExtractBoardId(string href)
    {
        var boardIdMatch = Regex.Match(href, @"/boards/(\d+)");
        if (!boardIdMatch.Success)
            return null;

        if (int.TryParse(boardIdMatch.Groups[1].Value, out var boardId))
            return boardId;

        return null;
    }

    private static string ExtractBoardName(HtmlAgilityPack.HtmlNode linkNode, int boardId)
    {
        var nameNode = linkNode.SelectSingleNode(".//span[@class='icon-label']");
        if (nameNode != null)
            return nameNode.InnerText.Trim();

        return $"Board {boardId}";
    }

    private static int ExtractCount(HtmlAgilityPack.HtmlNode row, string className)
    {
        var node = row.SelectSingleNode($".//td[@class='{className}']");
        if (node == null)
            return 0;

        if (int.TryParse(node.InnerText.Trim(), out var count))
            return count;

        return 0;
    }


    private static int? ParseProjectId(string identifier)
    {
        if (int.TryParse(identifier, out var id))
            return id;
        return null;
    }

    private static async Task ListTopicsAsync(string boardIdString, string? projectName, (string SessionCookie, string BaseUrl) auth)
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
        var topics = ParseTopicsFromHtml(html);

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

    private static List<Topic> ParseTopicsFromHtml(string html)
    {
        var topics = new List<Topic>();
        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        // フォーラムのトピックテーブルを探す
        var topicTable = doc.DocumentNode.SelectSingleNode("//table[@class='list messages']");
        if (topicTable == null)
        {
            return topics;
        }

        var rows = topicTable.SelectNodes(".//tbody/tr");
        if (rows == null)
        {
            return topics;
        }

        foreach (var row in rows)
        {
            var topic = ParseTopicFromRow(row);
            if (topic != null)
            {
                topics.Add(topic);
            }
        }

        return topics;
    }

    private static Topic? ParseTopicFromRow(HtmlNode row)
    {
        try
        {
            var topic = new Topic();

            // タイトルとURL
            var subjectCell = row.SelectSingleNode(".//td[@class='subject']");
            if (subjectCell == null) return null;

            var linkNode = subjectCell.SelectSingleNode(".//a");
            if (linkNode == null) return null;

            topic.Title = linkNode.InnerText.Trim();
            var href = linkNode.GetAttributeValue("href", "");

            // IDを抽出
            var match = Regex.Match(href, @"/messages/(\d+)");
            if (match.Success)
            {
                topic.Id = int.Parse(match.Groups[1].Value);
            }

            // 完全なURLを構築（相対URLの場合）
            if (!href.StartsWith("http"))
            {
                // BaseUrlは既にauth内にあるので、それを使う必要がある
                // ここでは相対URLをそのまま保存
                topic.Url = href;
            }
            else
            {
                topic.Url = href;
            }

            // スティッキーとロックの状態
            if (subjectCell.InnerHtml.Contains("sticky"))
            {
                topic.IsSticky = true;
            }
            if (subjectCell.InnerHtml.Contains("locked"))
            {
                topic.IsLocked = true;
            }

            // 作成者
            var authorCell = row.SelectSingleNode(".//td[@class='author']");
            if (authorCell != null)
            {
                topic.Author = authorCell.InnerText.Trim();
            }

            // 返信数
            var repliesCell = row.SelectSingleNode(".//td[@class='replies']");
            if (repliesCell != null && int.TryParse(repliesCell.InnerText.Trim(), out var replies))
            {
                topic.Replies = replies;
            }

            // 最終返信日時
            var lastReplyCell = row.SelectSingleNode(".//td[@class='last-reply']");
            if (lastReplyCell != null)
            {
                var dateText = lastReplyCell.InnerText.Trim();
                if (DateTime.TryParse(dateText, out var lastReply))
                {
                    topic.LastReply = lastReply;
                }
            }

            return topic;
        }
        catch
        {
            return null;
        }
    }

    private static async Task ViewTopicAsync(string boardIdString, string topicIdString, string? projectName, (string SessionCookie, string BaseUrl) auth)
    {
        if (!int.TryParse(topicIdString, out var topicId))
        {
            AnsiConsole.MarkupLine("[red]Invalid topic ID.[/]");
            return;
        }

        var topicUrl = $"{auth.BaseUrl}/messages/{topicId}";

        using var client = new HttpClient();
        client.DefaultRequestHeaders.Add("Cookie", auth.SessionCookie);

        var response = await client.GetAsync(topicUrl);
        if (!response.IsSuccessStatusCode)
        {
            AnsiConsole.MarkupLine($"[red]Failed to fetch topic: {response.StatusCode}[/]");
            return;
        }

        var html = await response.Content.ReadAsStringAsync();
        var topicDetail = ParseTopicDetailFromHtml(html);

        if (topicDetail == null)
        {
            AnsiConsole.MarkupLine("[red]Failed to parse topic details.[/]");
            return;
        }

        // トピックの詳細を表示
        var panel = new Panel($"[bold]{Markup.Escape(topicDetail.Title)}[/]\n\n" +
                              $"Author: {Markup.Escape(topicDetail.Author)}\n" +
                              $"Replies: {topicDetail.Replies.Count}\n\n" +
                              $"{Markup.Escape(topicDetail.Content)}");
        panel.Header = new PanelHeader($"Topic #{topicDetail.Id}");
        AnsiConsole.Write(panel);

        // 返信を表示
        if (topicDetail.Replies.Count > 0)
        {
            AnsiConsole.MarkupLine("\n[bold]Replies:[/]");
            foreach (var reply in topicDetail.Replies)
            {
                var replyPanel = new Panel($"{Markup.Escape(reply.Content)}");
                replyPanel.Header = new PanelHeader($"{reply.Author} - {reply.CreatedAt:yyyy-MM-dd HH:mm}");
                AnsiConsole.Write(replyPanel);
            }
        }
    }

    private static TopicDetail? ParseTopicDetailFromHtml(string html)
    {
        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        var topicDetail = new TopicDetail();

        // タイトル
        var titleNode = doc.DocumentNode.SelectSingleNode("//h2") ??
                       doc.DocumentNode.SelectSingleNode("//div[@class='subject']/h3");
        if (titleNode != null)
        {
            topicDetail.Title = titleNode.InnerText.Trim();
        }

        // 作成者と内容
        var messageNode = doc.DocumentNode.SelectSingleNode("//div[@id='content']//div[@class='message']");
        if (messageNode != null)
        {
            var authorNode = messageNode.SelectSingleNode(".//p[@class='author']") ??
                           messageNode.SelectSingleNode(".//span[@class='author']");
            if (authorNode != null)
            {
                topicDetail.Author = authorNode.InnerText.Trim();
            }

            var contentNode = messageNode.SelectSingleNode(".//div[@class='wiki']");
            if (contentNode != null)
            {
                topicDetail.Content = contentNode.InnerText.Trim();
            }
        }

        // 返信を取得
        var replyNodes = doc.DocumentNode.SelectNodes("//div[@id='replies']//div[@class='message reply']");
        if (replyNodes != null)
        {
            foreach (var replyNode in replyNodes)
            {
                var reply = new TopicReply();

                var replyAuthorNode = replyNode.SelectSingleNode(".//p[@class='author']") ??
                                     replyNode.SelectSingleNode(".//span[@class='author']");
                if (replyAuthorNode != null)
                {
                    reply.Author = replyAuthorNode.InnerText.Trim();
                }

                var replyContentNode = replyNode.SelectSingleNode(".//div[@class='wiki']");
                if (replyContentNode != null)
                {
                    reply.Content = replyContentNode.InnerText.Trim();
                }

                topicDetail.Replies.Add(reply);
            }
        }

        return topicDetail;
    }

    private static void DisplayInfo()
    {
        _logger?.LogDebug("Displaying extension information");

        Console.WriteLine("RedmineCLI Board Extension v1.0.0");
        Console.WriteLine();
        Console.WriteLine("This extension provides board management functionality for RedmineCLI.");
        Console.WriteLine("It supports form-based authentication for Redmine servers.");
        Console.WriteLine();
        Console.WriteLine("Available commands:");
        Console.WriteLine("  login    - Login to Redmine using username/password");
        Console.WriteLine("  list     - List all boards");
        Console.WriteLine("  logout   - Clear saved session");
        Console.WriteLine("  info     - Display this information");
        Console.WriteLine();
        Console.WriteLine("Environment variables:");

        var envVars = new[]
        {
            "REDMINE_URL",
            "REDMINE_API_KEY",
            "REDMINE_USER",
            "REDMINE_PROJECT",
            "REDMINE_CONFIG_DIR"
        };

        foreach (var envVar in envVars)
        {
            var value = Environment.GetEnvironmentVariable(envVar);
            if (!string.IsNullOrEmpty(value))
            {
                // Mask API key for security
                if (envVar == "REDMINE_API_KEY")
                {
                    value = value.Length > 4 ? value.Substring(0, 4) + "..." : "***";
                }
                Console.WriteLine($"  {envVar}: {value}");
                _logger?.LogDebug("Environment variable {Name}: {Value}", envVar, value);
            }
            else
            {
                Console.WriteLine($"  {envVar}: (not set)");
                _logger?.LogDebug("Environment variable {Name}: not set", envVar);
            }
        }
    }
}
