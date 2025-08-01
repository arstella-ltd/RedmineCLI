using System.CommandLine;

using Microsoft.Extensions.Logging;

using RedmineCLI.Exceptions;
using RedmineCLI.Formatters;
using RedmineCLI.Models;
using RedmineCLI.Services;

using Spectre.Console;

namespace RedmineCLI.Commands;

public class UserCommand
{
    private readonly IRedmineService _redmineService;
    private readonly IConfigService _configService;
    private readonly ITableFormatter _tableFormatter;
    private readonly IJsonFormatter _jsonFormatter;
    private readonly ILogger<UserCommand> _logger;

    public UserCommand(
        IRedmineService redmineService,
        IConfigService configService,
        ITableFormatter tableFormatter,
        IJsonFormatter jsonFormatter,
        ILogger<UserCommand> logger)
    {
        _redmineService = redmineService;
        _configService = configService;
        _tableFormatter = tableFormatter;
        _jsonFormatter = jsonFormatter;
        _logger = logger;
    }

    public static Command Create(
        IRedmineService redmineService,
        IConfigService configService,
        ITableFormatter tableFormatter,
        IJsonFormatter jsonFormatter,
        ILogger<UserCommand> logger)
    {
        var command = new Command("user", "Manage users");
        var userCommand = new UserCommand(redmineService, configService, tableFormatter, jsonFormatter, logger);

        var listCommand = new Command("list", "List users");
        listCommand.Aliases.Add("ls");

        var limitOption = new Option<int?>("--limit") { Description = "Limit the number of users (default: 30)" };
        limitOption.Aliases.Add("-L");
        var jsonOption = new Option<bool>("--json") { Description = "Output in JSON format" };
        var allOption = new Option<bool>("--all") { Description = "Show all details including email addresses" };
        allOption.Aliases.Add("-a");

        listCommand.Add(limitOption);
        listCommand.Add(jsonOption);
        listCommand.Add(allOption);

        listCommand.SetAction(async (parseResult) =>
        {
            var limit = parseResult.GetValue(limitOption);
            var json = parseResult.GetValue(jsonOption);
            var all = parseResult.GetValue(allOption);
            Environment.ExitCode = await userCommand.ListUsersAsync(limit, json, all);
        });

        command.Add(listCommand);
        return command;
    }

    private async Task<int> ListUsersAsync(int? limit, bool json, bool all)
    {
        try
        {
            _logger.LogDebug("Listing users with limit: {Limit}", limit);

            // 時刻フォーマット設定を読み込む
            var config = await _configService.LoadConfigAsync();
            var timeFormat = config.Preferences?.Time?.Format ?? "relative";
            _tableFormatter.SetTimeFormat(TimeFormat.Relative);

            if (timeFormat == "absolute")
            {
                _tableFormatter.SetTimeFormat(TimeFormat.Absolute);
            }
            else if (timeFormat == "utc")
            {
                _tableFormatter.SetTimeFormat(TimeFormat.Utc);
            }

            // デフォルトのlimitは30
            if (!limit.HasValue)
            {
                limit = 30;
            }

            var users = await _redmineService.GetUsersAsync(limit);

            if (json)
            {
                _jsonFormatter.FormatUsers(users, all);
            }
            else
            {
                _tableFormatter.FormatUsers(users, all);
            }

            return 0;
        }
        catch (RedmineApiException ex)
        {
            _logger.LogError(ex, "API error while listing users");

            if (ex.StatusCode == 403)
            {
                AnsiConsole.MarkupLine("[red]Error:[/] You do not have permission to view users.");
                return 1;
            }

            AnsiConsole.MarkupLine($"[red]Error:[/] {ex.Message}");
            return 1;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while listing users");
            AnsiConsole.MarkupLine($"[red]Error:[/] {ex.Message}");
            return 1;
        }
    }
}
