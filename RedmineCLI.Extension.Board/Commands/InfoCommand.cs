using System.CommandLine;

using Microsoft.Extensions.Logging;

namespace RedmineCLI.Extension.Board.Commands;

/// <summary>
/// 拡張機能の情報を表示するコマンド
/// </summary>
public class InfoCommand
{
    private readonly ILogger<InfoCommand> _logger;

    public InfoCommand(ILogger<InfoCommand> logger)
    {
        _logger = logger;
    }

    public Command Create()
    {
        var command = new Command("info", "Display extension and environment information");

        command.SetHandler(() =>
        {
            DisplayInfo();
        });

        return command;
    }

    private void DisplayInfo()
    {
        // _logger.LogDebug("Displaying extension information");

        Console.WriteLine("RedmineCLI Board Extension v1.0.0");
        Console.WriteLine();
        Console.WriteLine("This extension provides board management functionality for RedmineCLI.");
        Console.WriteLine("It supports form-based authentication for Redmine servers.");
        Console.WriteLine();
        Console.WriteLine("Available commands:");
        Console.WriteLine("  list     - List all boards");
        Console.WriteLine("  info     - Display this information");
        Console.WriteLine("  <board-id> topic list - List topics in a board");
        Console.WriteLine("  <board-id> topic <topic-id> - View topic details");
        Console.WriteLine();
        Console.WriteLine("Environment variables:");

        var envVars = new[]
        {
            "REDMINE_URL",
            "REDMINE_API_KEY",
            "REDMINE_USER",
            "REDMINE_PROJECT",
            "REDMINE_CONFIG_DIR"
        };

        foreach (var envVar in envVars)
        {
            var value = Environment.GetEnvironmentVariable(envVar);
            if (!string.IsNullOrEmpty(value))
            {
                // Mask API key for security
                if (envVar == "REDMINE_API_KEY")
                {
                    value = value.Length > 4 ? value.Substring(0, 4) + "..." : "***";
                }
                Console.WriteLine($"  {envVar}: {value}");
                // _logger.LogDebug("Environment variable {Name}: {Value}", envVar, value);
            }
            else
            {
                Console.WriteLine($"  {envVar}: (not set)");
                // _logger.LogDebug("Environment variable {Name}: not set", envVar);
            }
        }
    }
}
