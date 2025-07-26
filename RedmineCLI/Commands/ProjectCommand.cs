using System.CommandLine;

using Microsoft.Extensions.Logging;

using RedmineCLI.Exceptions;
using RedmineCLI.Formatters;
using RedmineCLI.Models;
using RedmineCLI.Services;

using Spectre.Console;

namespace RedmineCLI.Commands;

public class ProjectCommand
{
    private readonly IRedmineService _redmineService;
    private readonly IConfigService _configService;
    private readonly ITableFormatter _tableFormatter;
    private readonly IJsonFormatter _jsonFormatter;
    private readonly ILogger<ProjectCommand> _logger;

    public ProjectCommand(
        IRedmineService redmineService,
        IConfigService configService,
        ITableFormatter tableFormatter,
        IJsonFormatter jsonFormatter,
        ILogger<ProjectCommand> logger)
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
        ILogger<ProjectCommand> logger)
    {
        var command = new Command("project", "Manage projects");
        var projectCommand = new ProjectCommand(redmineService, configService, tableFormatter, jsonFormatter, logger);

        var listCommand = new Command("list", "List projects");
        listCommand.Aliases.Add("ls");

        var publicOption = new Option<bool>("--public") { Description = "Show only public projects" };
        var jsonOption = new Option<bool>("--json") { Description = "Output in JSON format" };

        listCommand.Add(publicOption);
        listCommand.Add(jsonOption);

        listCommand.SetAction(async (parseResult) =>
        {
            var publicOnly = parseResult.GetValue(publicOption);
            var json = parseResult.GetValue(jsonOption);
            Environment.ExitCode = await projectCommand.ListProjectsAsync(publicOnly, json);
        });

        command.Add(listCommand);
        return command;
    }

    private async Task<int> ListProjectsAsync(bool publicOnly, bool json)
    {
        try
        {
            _logger.LogDebug("Listing projects (public only: {PublicOnly})", publicOnly);

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

            var projects = await _redmineService.GetProjectsAsync();

            // TODO: Implement public filtering when API supports it
            // For now, we display all projects
            if (publicOnly)
            {
                _logger.LogDebug("Public filtering requested but not yet implemented");
            }

            if (json)
            {
                _jsonFormatter.FormatProjects(projects);
            }
            else
            {
                _tableFormatter.FormatProjects(projects);
            }

            return 0;
        }
        catch (RedmineApiException ex)
        {
            _logger.LogError(ex, "API error while listing projects");
            AnsiConsole.MarkupLine($"[red]Error:[/] {ex.Message}");
            return 1;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while listing projects");
            AnsiConsole.MarkupLine($"[red]Error:[/] {ex.Message}");
            return 1;
        }
    }
}
