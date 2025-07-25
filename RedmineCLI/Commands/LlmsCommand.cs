using System.CommandLine;
using System.Text;

using Microsoft.Extensions.Logging;

using Spectre.Console;

namespace RedmineCLI.Commands;

public class LlmsCommand
{
    private readonly ILogger<LlmsCommand> _logger;
    private readonly RootCommand? _rootCommand;

    public LlmsCommand(ILogger<LlmsCommand> logger, RootCommand? rootCommand = null)
    {
        _logger = logger;
        _rootCommand = rootCommand;
    }

    public static Command Create(ILogger<LlmsCommand> logger, RootCommand? rootCommand = null)
    {
        var command = new Command("llms", "Show LLM-friendly information about RedmineCLI");
        var llmsCommand = new LlmsCommand(logger, rootCommand);

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
            AnsiConsole.WriteLine("# RedmineCLI - Comprehensive Command Reference for LLMs");
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
            AnsiConsole.WriteLine("## All Available Options");
            AnsiConsole.WriteLine();

            // 動的にコマンドとオプションを抽出して表示
            if (_rootCommand != null)
            {
                DisplayCommandOptions(_rootCommand);
            }
            else
            {
                // フォールバック: 静的な情報を表示
                AnsiConsole.WriteLine("### Global Options");
                AnsiConsole.WriteLine("- `--help` : Show help and usage information");
                AnsiConsole.WriteLine("- `--version` : Show version information");
                AnsiConsole.WriteLine("- `--license` : Show license information");
                AnsiConsole.WriteLine("- `--verbose` : Show detailed error information including stack traces");
                AnsiConsole.WriteLine();
                DisplayStaticCommandOptions();
            }
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

    private void DisplayCommandOptions(RootCommand rootCommand)
    {
        // グローバルオプションを表示
        AnsiConsole.WriteLine("### Global Options");
        AnsiConsole.WriteLine();
        foreach (var option in rootCommand.Options.OrderBy(o => o.Name))
        {
            DisplayOption(option);
        }
        AnsiConsole.WriteLine();

        // 各コマンドとそのオプションを表示
        foreach (var subcommand in rootCommand.Subcommands.OrderBy(c => c.Name))
        {
            DisplayCommand(subcommand, "### ");
        }
    }

    private void DisplayCommand(Command command, string prefix)
    {
        AnsiConsole.WriteLine($"{prefix}`{command.Name}` Command");
        AnsiConsole.WriteLine();

        if (!string.IsNullOrEmpty(command.Description))
        {
            AnsiConsole.WriteLine($"Description: {command.Description}");
            AnsiConsole.WriteLine();
        }

        // サブコマンドがある場合
        if (command.Subcommands.Any())
        {
            AnsiConsole.WriteLine("Subcommands:");
            foreach (var subcommand in command.Subcommands.OrderBy(c => c.Name))
            {
                AnsiConsole.WriteLine($"- `{command.Name} {subcommand.Name}` - {subcommand.Description}");

                // サブコマンドのオプションを表示
                if (subcommand.Options.Any())
                {
                    AnsiConsole.WriteLine($"  Options for `{command.Name} {subcommand.Name}`:");
                    foreach (var option in subcommand.Options.OrderBy(o => o.Name))
                    {
                        DisplayOption(option, "  ");
                    }
                }
                AnsiConsole.WriteLine();
            }
        }

        // 直接のオプションがある場合
        if (command.Options.Any())
        {
            AnsiConsole.WriteLine($"Options for `{command.Name}`:");
            foreach (var option in command.Options.OrderBy(o => o.Name))
            {
                DisplayOption(option);
            }
            AnsiConsole.WriteLine();
        }
    }

    private void DisplayOption(Option option, string indent = "")
    {
        var sb = new StringBuilder();
        sb.Append($"{indent}- `{option.Name}`");

        if (option.Aliases.Any())
        {
            var aliases = string.Join(", ", option.Aliases.Select(a => $"`{a}`"));
            sb.Append($" (aliases: {aliases})");
        }

        AnsiConsole.WriteLine(sb.ToString());

        if (!string.IsNullOrEmpty(option.Description))
        {
            AnsiConsole.WriteLine($"{indent}  - Description: {option.Description}");
        }

        // オプションの型情報を表示
        var optionType = option.GetType();
        if (optionType.IsGenericType)
        {
            var genericArg = optionType.GetGenericArguments().FirstOrDefault();
            if (genericArg != null)
            {
                var typeName = GetFriendlyTypeName(genericArg);
                AnsiConsole.WriteLine($"{indent}  - Type: `{typeName}`");
            }
        }
    }

    private string GetFriendlyTypeName(Type type)
    {
        if (type == typeof(string))
            return "string";
        if (type == typeof(int) || type == typeof(int?))
            return "integer" + (type.IsGenericType ? " (optional)" : "");
        if (type == typeof(bool))
            return "boolean";
        if (type == typeof(bool?))
            return "boolean (optional)";

        return type.Name;
    }

    private void DisplayStaticCommandOptions()
    {
        // 静的なコマンドオプション情報を表示（フォールバック用）
        AnsiConsole.WriteLine("### Issue Command Options");
        AnsiConsole.WriteLine("- `-a, --assignee` : Filter by assignee (username, ID, or @me)");
        AnsiConsole.WriteLine("- `-s, --status` : Filter by status (open, closed, all, or status ID)");
        AnsiConsole.WriteLine("- `-p, --project` : Filter by project (identifier or ID)");
        AnsiConsole.WriteLine("- `-L, --limit` : Limit number of results (default: 30)");
        AnsiConsole.WriteLine("- `--offset` : Offset for pagination");
        AnsiConsole.WriteLine("- `--json` : Output in JSON format");
        AnsiConsole.WriteLine("- `--web, -w` : Open in web browser");
        AnsiConsole.WriteLine("- `--absolute-time` : Display absolute time instead of relative time");
        AnsiConsole.WriteLine();
        AnsiConsole.WriteLine("### Other Command Options");
        AnsiConsole.WriteLine("- Various command-specific options available for auth, config, and attachment commands");
    }
}
