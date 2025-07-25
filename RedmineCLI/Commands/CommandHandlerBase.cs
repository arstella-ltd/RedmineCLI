using System.Threading;

using Microsoft.Extensions.DependencyInjection;

using RedmineCLI.Services;

using Spectre.Console;

namespace RedmineCLI.Commands;

public static class CommandHandlerBase
{
    public static async Task<int> HandleAsync(Func<CancellationToken, Task<int>> action, IServiceProvider serviceProvider, CancellationToken cancellationToken = default)
    {
        try
        {
            return await action(cancellationToken);
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

            // Check if debug mode is enabled via environment or args
            var args = Environment.GetCommandLineArgs();
            var debugMode = args.Contains("--debug") || 
                           Environment.GetEnvironmentVariable("REDMINE_CLI_DEBUG") == "1";

            if (debugMode)
            {
                AnsiConsole.WriteLine();
                AnsiConsole.MarkupLine("[dim]--- Debug Information ---[/]");
                AnsiConsole.WriteException(ex);
            }

            return 1;
        }
    }
}