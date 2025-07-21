using System.CommandLine;
using System.Linq;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Logging;

using Polly;
using Polly.Extensions.Http;

using RedmineCLI.ApiClient;
using RedmineCLI.Commands;
using RedmineCLI.Formatters;
using RedmineCLI.Services;
using RedmineCLI.Utils;

using Spectre.Console;

namespace RedmineCLI;

public class Program
{
    public static async Task<int> Main(string[] args)
    {
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

        var authCommand = AuthCommand.Create(configService, apiClient, authLogger);
        var issueCommand = IssueCommand.Create(apiClient, configService, tableFormatter, jsonFormatter, issueLogger);
        var configCommand = new Command("config", "Manage configuration");

        rootCommand.Add(authCommand);
        rootCommand.Add(issueCommand);
        rootCommand.Add(configCommand);

        // Add global options
        var debugOption = new Option<bool>("--debug");
        debugOption.Description = "Enable debug output";
        debugOption.Aliases.Add("-d");
        rootCommand.Add(debugOption);

        // Note: Built-in --version option is provided by System.CommandLine

        // Add licenses option
        var licensesOption = new Option<bool>("--licenses");
        licensesOption.Description = "Show license information";
        rootCommand.Add(licensesOption);

        // Add root command action to handle global options
        rootCommand.SetAction(async (parseResult) =>
        {
            if (parseResult.GetValue(licensesOption))
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

            // Default action - no specific handling needed
        });

        // Create and use CLI configuration
        var config = new CommandLineConfiguration(rootCommand);

        // Parse and execute
        return await config.InvokeAsync(args);
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        // Logging
        services.AddLogging(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Warning);

            // Suppress HttpClient logs
            builder.AddFilter("System.Net.Http.HttpClient", LogLevel.Warning);
        });

        // Configuration services
        services.AddSingleton<IConfigService, ConfigService>();

        // HTTP Client (Polly retry policy to be added later)
        services.AddHttpClient<IRedmineApiClient, RedmineApiClient>();
        // TODO: Add Polly retry policy after resolving dependencies

        // Redmine API Client
        services.AddScoped<IRedmineApiClient, RedmineApiClient>();

        // Formatters
        services.AddSingleton<ITableFormatter, TableFormatter>();
        services.AddSingleton<IJsonFormatter, JsonFormatter>();

        // License Helper
        services.AddSingleton<ILicenseHelper, LicenseHelper>();
    }
}
