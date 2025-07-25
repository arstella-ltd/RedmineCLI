using System.CommandLine;
using System.IO.Abstractions;
using System.Linq;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Logging;


using RedmineCLI.ApiClient;
using RedmineCLI.Commands;
using RedmineCLI.Formatters;
using RedmineCLI.Services;
using RedmineCLI.Utils;

using Spectre.Console;

namespace RedmineCLI;

public class Program
{
    private static bool _debugMode = false;

    public static async Task<int> Main(string[] args)
    {
        // Check for debug flag early
        _debugMode = args.Contains("--debug");

        // Create service collection and configure DI
        var services = new ServiceCollection();
        ConfigureServices(services);

        var serviceProvider = services.BuildServiceProvider();

        // Create root command
        var rootCommand = new RootCommand("A GitHub CLI-like tool for managing Redmine tickets");

        // Configure commands
        var configService = serviceProvider.GetRequiredService<IConfigService>();
        var apiClient = serviceProvider.GetRequiredService<IRedmineApiClient>();
        var authLogger = serviceProvider.GetRequiredService<ILogger<AuthCommand>>();
        var issueLogger = serviceProvider.GetRequiredService<ILogger<IssueCommand>>();
        var tableFormatter = serviceProvider.GetRequiredService<ITableFormatter>();
        var jsonFormatter = serviceProvider.GetRequiredService<IJsonFormatter>();
        var licenseHelper = serviceProvider.GetRequiredService<ILicenseHelper>();
        var errorMessageService = serviceProvider.GetRequiredService<IErrorMessageService>();

        var authCommand = AuthCommand.Create(configService, apiClient, authLogger);
        var issueCommand = IssueCommand.Create(apiClient, configService, tableFormatter, jsonFormatter, issueLogger, errorMessageService);
        var configLogger = serviceProvider.GetRequiredService<ILogger<ConfigCommand>>();
        var configCommand = ConfigCommand.Create(configService, configLogger);
        var fileSystem = serviceProvider.GetRequiredService<IFileSystem>();
        var attachmentCommand = new AttachmentCommand().CreateCommand(configService, apiClient, tableFormatter, jsonFormatter, fileSystem);

        rootCommand.Add(authCommand);
        rootCommand.Add(issueCommand);
        rootCommand.Add(configCommand);
        rootCommand.Add(attachmentCommand);

        // Add global options
        // Note: Built-in --version option is provided by System.CommandLine

        // Add license option
        var licenseOption = new Option<bool>("--license");
        licenseOption.Description = "Show license information";
        rootCommand.Add(licenseOption);

        // Add debug option
        var debugOption = new Option<bool>("--debug");
        debugOption.Description = "Show detailed error information including stack traces";
        rootCommand.Add(debugOption);

        // Create and use CLI configuration (moved up to be in scope)
        var config = new CommandLineConfiguration(rootCommand)
        {
            // Disable response file processing to allow @me syntax
            ResponseFileTokenReplacer = null
        };

        // Add root command action to handle global options
        rootCommand.SetAction(async (parseResult) =>
        {
            if (parseResult.GetValue(licenseOption))
            {
                var licenses = await licenseHelper.GetLicenseInfoAsync();

                // Display main license info with Spectre.Console styling
                var mainPanel = new Panel($"[bold green]{licenses["RedmineCLI"].Name}[/] - [cyan]{licenses["RedmineCLI"].License.Split('\n')[0]}[/]\n\n[dim]Copyright (c) 2025 Arstella ltd.[/]")
                    .Header("[bold yellow]License Information[/]")
                    .Border(BoxBorder.Rounded);

                AnsiConsole.Write(mainPanel);
                AnsiConsole.WriteLine();

                // Display third-party licenses in a table
                var table = new Table()
                    .Title("[bold blue]Third-party Dependencies[/]")
                    .Border(TableBorder.Rounded)
                    .BorderColor(Color.Grey);

                table.AddColumn("[bold]Library[/]");
                table.AddColumn("[bold]Version[/]");
                table.AddColumn("[bold]License[/]");
                table.AddColumn("[bold]Project URL[/]");

                foreach (var license in licenses.Where(l => l.Key != "RedmineCLI"))
                {
                    table.AddRow(
                        $"[green]{license.Value.Name}[/]",
                        $"[cyan]{license.Value.Version}[/]",
                        $"[yellow]{license.Value.License.Split('\n')[0]}[/]",
                        $"[link]{license.Value.ProjectUrl}[/]"
                    );
                }

                AnsiConsole.Write(table);
                AnsiConsole.WriteLine();
                AnsiConsole.MarkupLine("[dim]See THIRD-PARTY-NOTICES.txt for full license texts.[/]");

                Environment.ExitCode = 0;
                return;
            }

            // Show help when no arguments are provided
            if (parseResult.CommandResult.Command == rootCommand &&
                parseResult.Tokens.Count == 0)
            {
                // Create a new parse result with --help to trigger help display
                var helpArgs = new[] { "--help" };
                config.InvokeAsync(helpArgs).Wait();
                Environment.ExitCode = 0;
                return;
            }

            // Default action - no specific handling needed
        });

        // Parse and execute with global exception handling
        try
        {
            return await config.InvokeAsync(args);
        }
        catch (Exception ex)
        {
            var errorService = serviceProvider.GetRequiredService<IErrorMessageService>();
            var errorMessage = errorService.GetUserFriendlyMessage(ex);
            var suggestion = errorService.GetSuggestion(ex);

            AnsiConsole.MarkupLine($"[red]Error:[/] {errorMessage}");
            
            if (suggestion != null)
            {
                AnsiConsole.MarkupLine($"[yellow]{suggestion}[/]");
            }

            if (_debugMode)
            {
                AnsiConsole.WriteLine();
                AnsiConsole.MarkupLine("[dim]--- Debug Information ---[/]");
                AnsiConsole.WriteException(ex);
            }

            return 1;
        }
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        // Logging
        services.AddLogging(builder =>
        {
            builder.AddConsole();
            
            // Set log level based on debug mode
            if (_debugMode)
            {
                builder.SetMinimumLevel(LogLevel.Debug);
                builder.AddFilter("System.Net.Http.HttpClient", LogLevel.Debug);
            }
            else
            {
                builder.SetMinimumLevel(LogLevel.Error);
                // Suppress HttpClient logs
                builder.AddFilter("System.Net.Http.HttpClient", LogLevel.Warning);
            }
        });

        // Configuration services
        services.AddSingleton<IConfigService, ConfigService>();

        // Error handling
        services.AddSingleton<IErrorMessageService, ErrorMessageService>();

        // HTTP Client (Polly retry policy to be added later)
        services.AddHttpClient<IRedmineApiClient, RedmineApiClient>();
        // TODO: Add Polly retry policy after resolving dependencies

        // Redmine API Client
        services.AddScoped<IRedmineApiClient, RedmineApiClient>();

        // Utils
        services.AddSingleton<ITimeHelper, TimeHelper>();

        // Formatters
        services.AddScoped<ITableFormatter, TableFormatter>();
        services.AddSingleton<IJsonFormatter, JsonFormatter>();

        // License Helper
        services.AddSingleton<ILicenseHelper, LicenseHelper>();

        // File System
        services.AddSingleton<IFileSystem, FileSystem>();
    }
}
