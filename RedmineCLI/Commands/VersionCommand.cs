using System.CommandLine;

using Microsoft.Extensions.Logging;

using RedmineCLI.Exceptions;
using RedmineCLI.Formatters;
using RedmineCLI.Services;

using Spectre.Console;

namespace RedmineCLI.Commands;

public class VersionCommand
{
    private readonly IRedmineService _redmineService;
    private readonly IConfigService _configService;
    private readonly ITableFormatter _tableFormatter;
    private readonly IJsonFormatter _jsonFormatter;
    private readonly ILogger<VersionCommand> _logger;

    public VersionCommand(
        IRedmineService redmineService,
        IConfigService configService,
        ITableFormatter tableFormatter,
        IJsonFormatter jsonFormatter,
        ILogger<VersionCommand> logger)
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
        ILogger<VersionCommand> logger)
    {
        var command = new Command("version", "Manage project versions");
        var versionCommand = new VersionCommand(redmineService, configService, tableFormatter, jsonFormatter, logger);

        var listCommand = new Command("list", "List project versions");
        listCommand.Aliases.Add("ls");

        var projectOption = new Option<string>("--project", "-p") { Description = "Project identifier (required)", Required = true };
        var jsonOption = new Option<bool>("--json") { Description = "Output in JSON format" };

        listCommand.Add(projectOption);
        listCommand.Add(jsonOption);

        listCommand.SetAction(async (parseResult) =>
        {
            var project = parseResult.GetValue(projectOption)!;
            var json = parseResult.GetValue(jsonOption);
            Environment.ExitCode = await versionCommand.ListVersionsAsync(project, json);
        });

        command.Add(listCommand);
        return command;
    }

    private async Task<int> ListVersionsAsync(string project, bool json)
    {
        try
        {
            _logger.LogDebug("Listing versions for project: {Project}", project);

            var versions = await _redmineService.GetVersionsAsync(project);

            if (json)
            {
                _jsonFormatter.FormatVersions(versions);
            }
            else
            {
                _tableFormatter.FormatVersions(versions);
            }

            return 0;
        }
        catch (RedmineApiException ex)
        {
            _logger.LogError(ex, "API error while listing versions");
            AnsiConsole.MarkupLine($"[red]Error:[/] {ex.Message}");
            return 1;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while listing versions");
            AnsiConsole.MarkupLine($"[red]Error:[/] {ex.Message}");
            return 1;
        }
    }
}
