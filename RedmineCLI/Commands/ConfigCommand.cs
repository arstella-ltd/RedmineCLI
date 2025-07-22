using System.CommandLine;

using Microsoft.Extensions.Logging;

using RedmineCLI.Services;

using Spectre.Console;

namespace RedmineCLI.Commands;

public class ConfigCommand
{
    private readonly IConfigService _configService;
    private readonly ILogger<ConfigCommand> _logger;

    public ConfigCommand(IConfigService configService, ILogger<ConfigCommand> logger)
    {
        _configService = configService;
        _logger = logger;
    }

    public static Command Create(IConfigService configService, ILogger<ConfigCommand> logger)
    {
        var command = new Command("config", "Manage configuration");
        var configCommand = new ConfigCommand(configService, logger);

        // Set command
        var setCommand = new Command("set", "Set a configuration value");
        var keyArgument = new Argument<string>("key");
        keyArgument.Description = "Configuration key (e.g., time.format)";
        var valueArgument = new Argument<string>("value");
        valueArgument.Description = "Configuration value";

        setCommand.Add(keyArgument);
        setCommand.Add(valueArgument);

        setCommand.SetAction(async (parseResult) =>
        {
            var key = parseResult.GetValue(keyArgument);
            var value = parseResult.GetValue(valueArgument);

            Environment.ExitCode = await configCommand.SetAsync(key, value, CancellationToken.None);
        });

        command.Add(setCommand);

        // Get command
        var getCommand = new Command("get", "Get a configuration value");
        var getKeyArgument = new Argument<string>("key");
        getKeyArgument.Description = "Configuration key";

        getCommand.Add(getKeyArgument);

        getCommand.SetAction(async (parseResult) =>
        {
            var key = parseResult.GetValue(getKeyArgument);

            Environment.ExitCode = await configCommand.GetAsync(key, CancellationToken.None);
        });

        command.Add(getCommand);

        // List command
        var listCommand = new Command("list", "List all configuration values");

        listCommand.SetAction(async (_) =>
        {
            Environment.ExitCode = await configCommand.ListAsync(CancellationToken.None);
        });

        command.Add(listCommand);

        // Set default action to list when no subcommand is provided
        command.SetAction(async (_) =>
        {
            Environment.ExitCode = await configCommand.ListAsync(CancellationToken.None);
        });

        return command;
    }

    public async Task<int> SetAsync(string key, string value, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Setting configuration key: {Key} = {Value}", key, value);

            var config = await _configService.LoadConfigAsync();

            // Handle different configuration keys
            switch (key.ToLower())
            {
                case "time.format":
                    var validFormats = new[] { "relative", "absolute", "utc" };
                    if (!validFormats.Contains(value.ToLower()))
                    {
                        AnsiConsole.MarkupLine($"[red]Error:[/] Invalid time format. Valid values are: {string.Join(", ", validFormats)}");
                        return 1;
                    }
                    config.Preferences.Time.Format = value.ToLower();
                    break;

                case "time.timezone":
                    config.Preferences.Time.Timezone = value;
                    break;

                case "defaultformat":
                    var validOutputFormats = new[] { "table", "json" };
                    if (!validOutputFormats.Contains(value.ToLower()))
                    {
                        AnsiConsole.MarkupLine($"[red]Error:[/] Invalid default format. Valid values are: {string.Join(", ", validOutputFormats)}");
                        return 1;
                    }
                    config.Preferences.DefaultFormat = value.ToLower();
                    break;

                case "pagesize":
                    if (!int.TryParse(value, out var pageSize) || pageSize < 1 || pageSize > 100)
                    {
                        AnsiConsole.MarkupLine("[red]Error:[/] Page size must be a number between 1 and 100");
                        return 1;
                    }
                    config.Preferences.PageSize = pageSize;
                    break;

                case "editor":
                    config.Preferences.Editor = value;
                    break;

                default:
                    AnsiConsole.MarkupLine($"[red]Error:[/] Unknown configuration key: {key}");
                    AnsiConsole.MarkupLine("[dim]Available keys: time.format, time.timezone, defaultformat, pagesize, editor[/]");
                    return 1;
            }

            await _configService.SaveConfigAsync(config);
            AnsiConsole.MarkupLine($"[green]✓[/] Configuration updated: {key} = {value}");

            return 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set configuration");
            AnsiConsole.MarkupLine($"[red]Error:[/] {ex.Message}");
            return 1;
        }
    }

    public async Task<int> GetAsync(string key, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Getting configuration key: {Key}", key);

            var config = await _configService.LoadConfigAsync();

            string? value = key.ToLower() switch
            {
                "time.format" => config.Preferences.Time.Format,
                "time.timezone" => config.Preferences.Time.Timezone,
                "defaultformat" => config.Preferences.DefaultFormat,
                "pagesize" => config.Preferences.PageSize.ToString(),
                "editor" => config.Preferences.Editor,
                _ => null
            };

            if (value == null)
            {
                AnsiConsole.MarkupLine($"[red]Error:[/] Unknown configuration key: {key}");
                return 1;
            }

            AnsiConsole.WriteLine(value);
            return 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get configuration");
            AnsiConsole.MarkupLine($"[red]Error:[/] {ex.Message}");
            return 1;
        }
    }

    public async Task<int> ListAsync(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Listing all configuration values");

            var config = await _configService.LoadConfigAsync();

            AnsiConsole.MarkupLine("[bold]Configuration Settings:[/]");
            AnsiConsole.WriteLine();

            var table = new Table();
            table.AddColumn("Key");
            table.AddColumn("Value");

            // Preferences
            table.AddRow("time.format", config.Preferences.Time.Format);
            table.AddRow("time.timezone", config.Preferences.Time.Timezone);
            table.AddRow("defaultformat", config.Preferences.DefaultFormat);
            table.AddRow("pagesize", config.Preferences.PageSize.ToString());
            table.AddRow("editor", config.Preferences.Editor ?? "[dim](not set)[/]");

            AnsiConsole.Write(table);

            // Current profile info
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine($"[bold]Current Profile:[/] {config.CurrentProfile}");

            if (config.Profiles.TryGetValue(config.CurrentProfile, out var profile))
            {
                AnsiConsole.MarkupLine($"[dim]Server URL: {profile.Url}[/]");
            }

            return 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to list configuration");
            AnsiConsole.MarkupLine($"[red]Error:[/] {ex.Message}");
            return 1;
        }
    }
}
