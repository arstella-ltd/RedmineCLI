using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

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
        // TODO: Add commands here
        var authCommand = new Command("auth", "Authenticate with Redmine server");
        var issueCommand = new Command("issue", "Manage Redmine issues");
        var configCommand = new Command("config", "Manage configuration");
        
        rootCommand.Add(authCommand);
        rootCommand.Add(issueCommand);
        rootCommand.Add(configCommand);
        
        // Add global options
        var debugOption = new Option<bool>("--debug");
        debugOption.Description = "Enable debug output";
        debugOption.Aliases.Add("-d");
        rootCommand.Add(debugOption);
        
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
            builder.SetMinimumLevel(LogLevel.Information);
        });
        
        // TODO: Register services
        // services.AddSingleton<IConfigService, ConfigService>();
        // services.AddSingleton<IRedmineApiClient, RedmineApiClient>();
        // services.AddSingleton<IRedmineService, RedmineService>();
        
        // HTTP Client
        services.AddHttpClient();
    }
}
