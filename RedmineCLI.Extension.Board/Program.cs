using System;
using System.CommandLine;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

using Microsoft.Extensions.Logging;

using RedmineCLI.Common.Models;
using RedmineCLI.Common.Services;
using RedmineCLI.Extension.Board.Models;

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

        // List command
        var listCommand = new Command("list", "List all boards (requires 'redmine auth login' first)");
        var projectOption = new Option<string>("--project", "Filter by project name or ID");
        var urlOption = new Option<string>("--url", "Redmine server URL (optional, uses stored credentials by default)");
        listCommand.Add(projectOption);
        listCommand.Add(urlOption);
        listCommand.SetHandler(async (string? project, string? url) =>
        {
            await ListBoardsAsync(project, url);
        }, projectOption, urlOption);

        // Info command
        var infoCommand = new Command("info", "Display extension and environment information");
        infoCommand.SetHandler(() =>
        {
            DisplayInfo();
        });

        rootCommand.AddCommand(listCommand);
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
            Console.Error.WriteLine("Error: No Redmine URL found. Please specify --url or run 'redmine auth login' first.");
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
                Console.Error.WriteLine($"Error: No credentials found for {redmineUrl}. Please run 'redmine auth login' first.");
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
            Console.Error.WriteLine("Error: Failed to authenticate. Please run 'redmine auth login' again.");
            Environment.Exit(1);
            return (redmineUrl, null);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error accessing keychain");
            Console.Error.WriteLine($"Error accessing keychain: {ex.Message}");
            Console.Error.WriteLine("Please run 'redmine auth login' first.");
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
            Console.Error.WriteLine("Error: Could not create session. Please ensure you have saved password credentials with 'redmine auth login --save-password'.");
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
                Console.Error.WriteLine("Error: Project identifier is required. Please specify --project option.");
                Console.Error.WriteLine("Usage: redmine-board list --project <project-identifier>");
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
                    Console.Error.WriteLine($"Error: No boards found for project '{projectFilter}'.");
                    Console.Error.WriteLine("Note: Board functionality requires a board plugin to be installed on the Redmine server.");
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    _logger?.LogError("Session expired or invalid");
                    Console.Error.WriteLine("Error: Session expired. Please run 'redmine auth login --save-password' again.");
                    Environment.Exit(1);
                    return;
                }
            }
            catch (HttpRequestException ex)
            {
                _logger?.LogError(ex, "Failed to fetch board URL");
                Console.Error.WriteLine($"Error: Failed to connect to Redmine server: {ex.Message}");
                Environment.Exit(1);
                return;
            }

            // Display results
            if (allBoards.Any())
            {
                Console.WriteLine($"\nFound {allBoards.Count} board(s):\n");
                Console.WriteLine($"{"ID",-6} {"Name",-30} {"Project",-20} {"Columns",-8} {"Cards",-8}");
                Console.WriteLine(new string('-', 75));

                foreach (var board in allBoards.DistinctBy(b => b.Id))
                {
                    Console.WriteLine($"{board.Id,-6} {board.Name,-30} {board.ProjectName ?? "N/A",-20} {board.ColumnCount,-8} {board.CardCount,-8}");
                }

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
                Console.WriteLine("No boards found.");
                Console.WriteLine("\nNote: Board functionality usually requires a plugin to be installed on the Redmine server.");
                Console.WriteLine("Common plugins: Redmine Agile, Backlogs, or custom board plugins.");
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
            // Look for common board patterns in HTML
            // Pattern 1: Links with "board" in href
            var boardLinkPattern = @"<a[^>]+href=[""']([^""']*board[^""']*)[""'][^>]*>([^<]+)</a>";
            var boardMatches = Regex.Matches(html, boardLinkPattern, RegexOptions.IgnoreCase);

            foreach (Match match in boardMatches)
            {
                var url = match.Groups[1].Value;
                var name = match.Groups[2].Value.Trim();

                // Skip navigation links
                if (name.ToLower().Contains("back") || name.ToLower().Contains("new") ||
                    name.ToLower().Contains("create") || string.IsNullOrWhiteSpace(name))
                    continue;

                // Extract board ID from URL if possible
                var idMatch = Regex.Match(url, @"/boards?/(\d+)");
                var boardId = idMatch.Success ? int.Parse(idMatch.Groups[1].Value) : boards.Count + 1;

                // Make URL absolute
                if (!url.StartsWith("http"))
                {
                    url = url.StartsWith("/") ? $"{baseUrl}{url}" : $"{baseUrl}/{url}";
                }

                var board = new Models.Board
                {
                    Id = boardId,
                    Name = System.Net.WebUtility.HtmlDecode(name),
                    Url = url,
                    ColumnCount = 0,
                    CardCount = 0
                };

                boards.Add(board);
                _logger?.LogDebug("Found board: {Name} (ID: {Id}, URL: {Url})", board.Name, board.Id, board.Url);
            }

            // Pattern 2: Divs or sections with class containing "board"
            var boardDivPattern = @"<div[^>]+class=[""'][^""']*board[^""']*[""'][^>]*>.*?<h\d[^>]*>([^<]+)</h\d>";
            var divMatches = Regex.Matches(html, boardDivPattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);

            foreach (Match match in divMatches)
            {
                var name = match.Groups[1].Value.Trim();
                if (!boards.Any(b => b.Name == name))
                {
                    var board = new Models.Board
                    {
                        Id = boards.Count + 1,
                        Name = System.Net.WebUtility.HtmlDecode(name),
                        Url = baseUrl,
                        ColumnCount = 0,
                        CardCount = 0
                    };
                    boards.Add(board);
                    _logger?.LogDebug("Found board from div: {Name}", board.Name);
                }
            }

            // Pattern 3: Table rows with board information
            var tablePattern = @"<tr[^>]*>.*?boards?/(\d+).*?<td[^>]*>([^<]+)</td>";
            var tableMatches = Regex.Matches(html, tablePattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);

            foreach (Match match in tableMatches)
            {
                var boardId = int.Parse(match.Groups[1].Value);
                var name = match.Groups[2].Value.Trim();

                if (!boards.Any(b => b.Id == boardId))
                {
                    var board = new Models.Board
                    {
                        Id = boardId,
                        Name = System.Net.WebUtility.HtmlDecode(name),
                        Url = $"{baseUrl}/boards/{boardId}",
                        ColumnCount = 0,
                        CardCount = 0
                    };
                    boards.Add(board);
                    _logger?.LogDebug("Found board from table: {Name} (ID: {Id})", board.Name, board.Id);
                }
            }

            // Try to extract column and card counts if visible
            foreach (var board in boards)
            {
                // Look for column count near board name
                var columnPattern = $@"{Regex.Escape(board.Name)}.*?(\d+)\s*columns?";
                var columnMatch = Regex.Match(html, columnPattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);
                if (columnMatch.Success)
                {
                    board.ColumnCount = int.Parse(columnMatch.Groups[1].Value);
                }

                // Look for card/issue count
                var cardPattern = $@"{Regex.Escape(board.Name)}.*?(\d+)\s*(cards?|issues?)";
                var cardMatch = Regex.Match(html, cardPattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);
                if (cardMatch.Success)
                {
                    board.CardCount = int.Parse(cardMatch.Groups[1].Value);
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


    private static int? ParseProjectId(string identifier)
    {
        if (int.TryParse(identifier, out var id))
            return id;
        return null;
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
