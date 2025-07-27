using System.CommandLine;

using Microsoft.Extensions.Logging;

using RedmineCLI.Exceptions;
using RedmineCLI.Formatters;
using RedmineCLI.Models;
using RedmineCLI.Services;

using Spectre.Console;

namespace RedmineCLI.Commands;

public class PriorityCommand
{
    private readonly IRedmineService _redmineService;
    private readonly IConfigService _configService;
    private readonly ITableFormatter _tableFormatter;
    private readonly IJsonFormatter _jsonFormatter;
    private readonly ILogger<PriorityCommand> _logger;

    public PriorityCommand(
        IRedmineService redmineService,
        IConfigService configService,
        ITableFormatter tableFormatter,
        IJsonFormatter jsonFormatter,
        ILogger<PriorityCommand> logger)
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
        ILogger<PriorityCommand> logger)
    {
        var command = new Command("priority", "Manage priorities");
        var priorityCommand = new PriorityCommand(redmineService, configService, tableFormatter, jsonFormatter, logger);

        var listCommand = new Command("list", "List priorities");
        listCommand.Aliases.Add("ls");

        var jsonOption = new Option<bool>("--json") { Description = "Output in JSON format" };

        listCommand.Add(jsonOption);

        listCommand.SetAction(async (parseResult) =>
        {
            var json = parseResult.GetValue(jsonOption);
            Environment.ExitCode = await priorityCommand.ListPrioritiesAsync(json);
        });

        command.Add(listCommand);
        return command;
    }

    private async Task<int> ListPrioritiesAsync(bool json)
    {
        try
        {
            _logger.LogDebug("Listing priorities");

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

            var priorities = await _redmineService.GetPrioritiesAsync();

            if (json)
            {
                _jsonFormatter.FormatPriorities(priorities);
            }
            else
            {
                _tableFormatter.FormatPriorities(priorities);
            }

            return 0;
        }
        catch (RedmineApiException ex)
        {
            _logger.LogError(ex, "API error while listing priorities");

            if (ex.StatusCode == 403)
            {
                AnsiConsole.MarkupLine("[red]Error:[/] You do not have permission to view priorities.");
                return 1;
            }

            AnsiConsole.MarkupLine($"[red]Error:[/] {ex.Message}");
            return 1;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while listing priorities");
            AnsiConsole.MarkupLine($"[red]Error:[/] {ex.Message}");
            return 1;
        }
    }
}