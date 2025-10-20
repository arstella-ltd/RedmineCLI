using System.CommandLine;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using RedmineCLI.Common.Services;
using RedmineCLI.Extension.Board.Commands;
using RedmineCLI.Extension.Board.Services;

using Spectre.Console;

namespace RedmineCLI.Extension.Board;

/// <summary>
/// RedmineCLI Board Extension - Provides board management functionality with form-based login
/// </summary>
public class Program
{
    public static async Task<int> Main(string[] args)
    {
        // Setup DI container
        var services = new ServiceCollection();
        ConfigureServices(services);

        var serviceProvider = services.BuildServiceProvider();
        // var logger = serviceProvider.GetRequiredService<ILogger<Program>>();

        // logger.LogDebug("Starting RedmineCLI Board Extension");
        // logger.LogDebug("Arguments: {Args}", string.Join(" ", args));

        // Create root command
        var rootCommand = new RootCommand("RedmineCLI Board Extension - Manage Redmine boards");

        // Create commands
        var boardListCommand = serviceProvider.GetRequiredService<BoardListCommand>();
        var infoCommand = serviceProvider.GetRequiredService<InfoCommand>();
        var boardTopicCommand = serviceProvider.GetRequiredService<BoardTopicCommand>();
        var viewCommand = serviceProvider.GetRequiredService<ViewCommand>();
        var replyCommand = serviceProvider.GetRequiredService<ReplyCommand>();
        var commentCommand = serviceProvider.GetRequiredService<CommentCommand>();

        // Add new commands
        rootCommand.AddCommand(boardListCommand.Create());
        rootCommand.AddCommand(viewCommand.Create());
        rootCommand.AddCommand(replyCommand.Create());
        rootCommand.AddCommand(commentCommand.Create());
        rootCommand.AddCommand(infoCommand.Create());

        // Handle dynamic board ID commands for backward compatibility (e.g., "redmine-board 21 topic list")
        var dynamicBoardCommand = boardTopicCommand.CreateDynamicBoardCommand(args);
        if (dynamicBoardCommand != null)
        {
            rootCommand.AddCommand(dynamicBoardCommand);
        }

        var result = await rootCommand.InvokeAsync(args);
        // logger.LogDebug("Extension exiting with code: {ExitCode}", result);
        return result;
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        // Logging
        services.AddLogging(builder =>
        {
            builder
                .AddConsole()
                .SetMinimumLevel(LogLevel.Warning);
        });

        // Common services
        services.AddSingleton<ICredentialStore>(provider => CredentialStore.Create());
        services.AddSingleton<IAnsiConsole>(AnsiConsole.Console);

        // Services
        services.AddScoped<IAuthenticationService, AuthenticationService>();
        services.AddScoped<IHtmlParsingService, HtmlParsingService>();
        services.AddScoped<IBoardService, BoardService>();

        // Commands
        services.AddScoped<BoardListCommand>();
        services.AddScoped<InfoCommand>();
        services.AddScoped<BoardTopicCommand>();
        services.AddScoped<ViewCommand>();
        services.AddScoped<ReplyCommand>();
        services.AddScoped<CommentCommand>();
    }
}
