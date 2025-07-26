using System.CommandLine;

using Microsoft.Extensions.Logging;

using RedmineCLI.Exceptions;
using RedmineCLI.Formatters;
using RedmineCLI.Services;

using Spectre.Console;

namespace RedmineCLI.Commands;

public class StatusCommand
{
    private readonly IRedmineService _redmineService;
    private readonly IConfigService _configService;
    private readonly ITableFormatter _tableFormatter;
    private readonly IJsonFormatter _jsonFormatter;
    private readonly ILogger<StatusCommand> _logger;

    public StatusCommand(
        IRedmineService redmineService,
        IConfigService configService,
        ITableFormatter tableFormatter,
        IJsonFormatter jsonFormatter,
        ILogger<StatusCommand> logger)
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
        ILogger<StatusCommand> logger)
    {
        var command = new Command("status", "Manage issue statuses");
        var statusCommand = new StatusCommand(redmineService, configService, tableFormatter, jsonFormatter, logger);

        var listCommand = new Command("list", "List issue statuses");
        listCommand.Aliases.Add("ls");

        var jsonOption = new Option<bool>("--json") { Description = "Output in JSON format" };

        listCommand.Add(jsonOption);

        listCommand.SetAction(async (parseResult) =>
        {
            var json = parseResult.GetValue(jsonOption);
            Environment.ExitCode = await statusCommand.ListStatusesAsync(json);
        });

        command.Add(listCommand);
        return command;
    }

    private async Task<int> ListStatusesAsync(bool json)
    {
        try
        {
            _logger.LogDebug("Listing issue statuses");

            var statuses = await _redmineService.GetIssueStatusesAsync();

            if (json)
            {
                _jsonFormatter.FormatIssueStatuses(statuses);
            }
            else
            {
                _tableFormatter.FormatIssueStatuses(statuses);
            }

            return 0;
        }
        catch (RedmineApiException ex)
        {
            _logger.LogError(ex, "API error while listing statuses");
            AnsiConsole.MarkupLine($"[red]Error:[/] {ex.Message}");
            return 1;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while listing statuses");
            AnsiConsole.MarkupLine($"[red]Error:[/] {ex.Message}");
            return 1;
        }
    }
}
