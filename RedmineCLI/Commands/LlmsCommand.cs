using System.CommandLine;

using Microsoft.Extensions.Logging;

using Spectre.Console;

namespace RedmineCLI.Commands;

public class LlmsCommand
{
    private readonly ILogger<LlmsCommand> _logger;

    public LlmsCommand(ILogger<LlmsCommand> logger)
    {
        _logger = logger;
    }

    public static Command Create(ILogger<LlmsCommand> logger)
    {
        var command = new Command("llms", "Show LLM-friendly information about RedmineCLI");
        var llmsCommand = new LlmsCommand(logger);

        command.SetAction(async (_) =>
        {
            Environment.ExitCode = await llmsCommand.ShowLlmsInfoAsync(CancellationToken.None);
        });

        return command;
    }

    public async Task<int> ShowLlmsInfoAsync(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Showing LLMs information");

            // LLMs.txt形式でRedmineCLIの情報を出力
            AnsiConsole.WriteLine("# RedmineCLI");
            AnsiConsole.WriteLine();
            AnsiConsole.WriteLine("RedmineCLI is a command-line interface tool for managing Redmine tickets, designed to provide a GitHub CLI-like experience.");
            AnsiConsole.WriteLine();
            AnsiConsole.WriteLine("## Installation");
            AnsiConsole.WriteLine();
            AnsiConsole.WriteLine("```bash");
            AnsiConsole.WriteLine("# Homebrew (macOS/Linux)");
            AnsiConsole.WriteLine("brew tap arstella-ltd/homebrew-tap");
            AnsiConsole.WriteLine("brew install redmine");
            AnsiConsole.WriteLine("```");
            AnsiConsole.WriteLine();
            AnsiConsole.WriteLine("## Authentication");
            AnsiConsole.WriteLine();
            AnsiConsole.WriteLine("First, authenticate with your Redmine server:");
            AnsiConsole.WriteLine();
            AnsiConsole.WriteLine("```bash");
            AnsiConsole.WriteLine("redmine auth login");
            AnsiConsole.WriteLine("```");
            AnsiConsole.WriteLine();
            AnsiConsole.WriteLine("## Core Commands");
            AnsiConsole.WriteLine();
            AnsiConsole.WriteLine("### Issue Management");
            AnsiConsole.WriteLine();
            AnsiConsole.WriteLine("```bash");
            AnsiConsole.WriteLine("# List issues (default: all issues in project)");
            AnsiConsole.WriteLine("redmine issue list");
            AnsiConsole.WriteLine();
            AnsiConsole.WriteLine("# List issues assigned to me");
            AnsiConsole.WriteLine("redmine issue list -a @me");
            AnsiConsole.WriteLine();
            AnsiConsole.WriteLine("# List issues with specific status");
            AnsiConsole.WriteLine("redmine issue list -s open");
            AnsiConsole.WriteLine("redmine issue list -s closed");
            AnsiConsole.WriteLine();
            AnsiConsole.WriteLine("# View issue details");
            AnsiConsole.WriteLine("redmine issue view <issue-id>");
            AnsiConsole.WriteLine();
            AnsiConsole.WriteLine("# Create new issue");
            AnsiConsole.WriteLine("redmine issue create");
            AnsiConsole.WriteLine();
            AnsiConsole.WriteLine("# Edit issue");
            AnsiConsole.WriteLine("redmine issue edit <issue-id> --status=closed");
            AnsiConsole.WriteLine("redmine issue edit <issue-id> --assigned-to=@me");
            AnsiConsole.WriteLine();
            AnsiConsole.WriteLine("# Add comment to issue");
            AnsiConsole.WriteLine("redmine issue comment <issue-id> -m \"Comment text\"");
            AnsiConsole.WriteLine("```");
            AnsiConsole.WriteLine();
            AnsiConsole.WriteLine("### Attachment Management");
            AnsiConsole.WriteLine();
            AnsiConsole.WriteLine("```bash");
            AnsiConsole.WriteLine("# List attachments for an issue");
            AnsiConsole.WriteLine("redmine issue attachment list <issue-id>");
            AnsiConsole.WriteLine();
            AnsiConsole.WriteLine("# Download attachment");
            AnsiConsole.WriteLine("redmine attachment download <attachment-id>");
            AnsiConsole.WriteLine();
            AnsiConsole.WriteLine("# View attachment details");
            AnsiConsole.WriteLine("redmine attachment view <attachment-id>");
            AnsiConsole.WriteLine("```");
            AnsiConsole.WriteLine();
            AnsiConsole.WriteLine("### Configuration");
            AnsiConsole.WriteLine();
            AnsiConsole.WriteLine("```bash");
            AnsiConsole.WriteLine("# Set configuration values");
            AnsiConsole.WriteLine("redmine config set time.format relative");
            AnsiConsole.WriteLine("redmine config set defaultformat json");
            AnsiConsole.WriteLine();
            AnsiConsole.WriteLine("# Get configuration value");
            AnsiConsole.WriteLine("redmine config get time.format");
            AnsiConsole.WriteLine();
            AnsiConsole.WriteLine("# List all configuration");
            AnsiConsole.WriteLine("redmine config list");
            AnsiConsole.WriteLine("```");
            AnsiConsole.WriteLine();
            AnsiConsole.WriteLine("## Options");
            AnsiConsole.WriteLine();
            AnsiConsole.WriteLine("### Global Options");
            AnsiConsole.WriteLine("- `--help` : Show help");
            AnsiConsole.WriteLine("- `--version` : Show version");
            AnsiConsole.WriteLine("- `--license` : Show license information");
            AnsiConsole.WriteLine();
            AnsiConsole.WriteLine("### Common Issue Options");
            AnsiConsole.WriteLine("- `-a, --assigned-to` : Filter by assignee (@me for current user)");
            AnsiConsole.WriteLine("- `-s, --status` : Filter by status (open, closed, all)");
            AnsiConsole.WriteLine("- `-p, --project` : Filter by project ID");
            AnsiConsole.WriteLine("- `-L, --limit` : Number of results to show");
            AnsiConsole.WriteLine("- `--json` : Output in JSON format (default: table)");
            AnsiConsole.WriteLine("- `--web` : Open in web browser");
            AnsiConsole.WriteLine();
            AnsiConsole.WriteLine("## Features");
            AnsiConsole.WriteLine();
            AnsiConsole.WriteLine("- Native AOT compiled for fast startup (<100ms)");
            AnsiConsole.WriteLine("- Cross-platform (Windows, macOS, Linux)");
            AnsiConsole.WriteLine("- Multiple profile support");
            AnsiConsole.WriteLine("- Sixel protocol support for inline image display");
            AnsiConsole.WriteLine("- JSON output format option (--json)");
            AnsiConsole.WriteLine("- Editor integration for comments");
            AnsiConsole.WriteLine("- Interactive and non-interactive modes");
            AnsiConsole.WriteLine();
            AnsiConsole.WriteLine("## Configuration Files");
            AnsiConsole.WriteLine();
            AnsiConsole.WriteLine("- Windows: `%APPDATA%\\redmine\\config.yml`");
            AnsiConsole.WriteLine("- macOS/Linux: `~/.config/redmine/config.yml`");
            AnsiConsole.WriteLine();
            AnsiConsole.WriteLine("## API Requirements");
            AnsiConsole.WriteLine();
            AnsiConsole.WriteLine("- Redmine REST API v3.0 or higher");
            AnsiConsole.WriteLine("- API key authentication");
            AnsiConsole.WriteLine();
            AnsiConsole.WriteLine("## Common Workflows");
            AnsiConsole.WriteLine();
            AnsiConsole.WriteLine("### Check my assigned issues");
            AnsiConsole.WriteLine("```bash");
            AnsiConsole.WriteLine("redmine issue list -a @me -s open");
            AnsiConsole.WriteLine("```");
            AnsiConsole.WriteLine();
            AnsiConsole.WriteLine("### Update issue status with comment");
            AnsiConsole.WriteLine("```bash");
            AnsiConsole.WriteLine("redmine issue edit 12345 --status=closed");
            AnsiConsole.WriteLine("redmine issue comment 12345 -m \"Fixed in commit abc123\"");
            AnsiConsole.WriteLine("```");
            AnsiConsole.WriteLine();
            AnsiConsole.WriteLine("### Create and assign issue");
            AnsiConsole.WriteLine("```bash");
            AnsiConsole.WriteLine("redmine issue create --title=\"Bug fix\" --assigned-to=@me");
            AnsiConsole.WriteLine("```");

            await Task.CompletedTask;
            return 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to show LLMs information");
            AnsiConsole.MarkupLine($"[red]Error:[/] {ex.Message}");
            return 1;
        }
    }
}
