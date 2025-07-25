using System.CommandLine;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace RedmineCLI.Commands;

public class LlmsCommand
{
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(LlmsCommand))]
    public static Command Create()
    {
        var command = new Command("llms", "Generate comprehensive documentation for LLMs");
        command.Description = "Output detailed information about all commands and options for LLMs to understand RedmineCLI capabilities";

        command.SetAction((parseResult) =>
        {
            var rootCommand = parseResult.RootCommandResult.Command;
            var output = GenerateComprehensiveDocumentation(rootCommand);
            Console.WriteLine(output);
            Environment.ExitCode = 0;
        });

        return command;
    }

    private static string GenerateComprehensiveDocumentation(Command rootCommand)
    {
        var sb = new StringBuilder();
        
        sb.AppendLine("# RedmineCLI - Comprehensive Command Reference for LLMs");
        sb.AppendLine();
        sb.AppendLine("RedmineCLI is a command-line interface for managing Redmine tickets, designed to provide a GitHub CLI-like experience.");
        sb.AppendLine();
        
        // グローバルオプション
        sb.AppendLine("## Global Options");
        sb.AppendLine();
        sb.AppendLine("These options are available for all commands:");
        sb.AppendLine();
        
        foreach (var option in rootCommand.Options)
        {
            AppendOptionDetails(sb, option);
        }
        
        sb.AppendLine();
        sb.AppendLine("## Available Commands");
        sb.AppendLine();
        
        // すべてのコマンドを再帰的に処理
        foreach (var subCommand in rootCommand.Subcommands)
        {
            AppendCommandDetails(sb, subCommand, 1);
        }
        
        sb.AppendLine();
        sb.AppendLine("## Usage Examples");
        sb.AppendLine();
        AppendUsageExamples(sb);
        
        return sb.ToString();
    }
    
    private static void AppendCommandDetails(StringBuilder sb, Command command, int level)
    {
        var indent = new string('#', level + 2);
        sb.AppendLine($"{indent} {command.Name}");
        sb.AppendLine();
        
        if (!string.IsNullOrEmpty(command.Description))
        {
            sb.AppendLine(command.Description);
            sb.AppendLine();
        }
        
        // コマンド固有のオプション
        if (command.Options.Any())
        {
            sb.AppendLine($"**Options for `{command.Name}`:**");
            sb.AppendLine();
            
            foreach (var option in command.Options)
            {
                AppendOptionDetails(sb, option);
            }
            sb.AppendLine();
        }
        
        // 引数
        if (command.Arguments.Any())
        {
            sb.AppendLine($"**Arguments for `{command.Name}`:**");
            sb.AppendLine();
            
            foreach (var argument in command.Arguments)
            {
                sb.AppendLine($"- `{argument.Name}` - {argument.Description ?? "No description"}");
                // Arguments are required by default in System.CommandLine
                sb.AppendLine("  - **Required**");
                sb.AppendLine();
            }
        }
        
        // サブコマンド
        if (command.Subcommands.Any())
        {
            sb.AppendLine($"**Subcommands:**");
            sb.AppendLine();
            
            foreach (var subCommand in command.Subcommands)
            {
                sb.AppendLine($"- `{subCommand.Name}` - {subCommand.Description ?? "No description"}");
            }
            sb.AppendLine();
            
            // サブコマンドの詳細を再帰的に追加
            foreach (var subCommand in command.Subcommands)
            {
                AppendCommandDetails(sb, subCommand, level + 1);
            }
        }
    }
    
    private static void AppendOptionDetails(StringBuilder sb, Option option)
    {
        var optionName = $"--{option.Name}";
        if (option.Aliases.Any())
        {
            var aliases = string.Join(", ", option.Aliases.Select(a => a.StartsWith("--") ? a : $"-{a}"));
            optionName = $"{optionName}, {aliases}";
        }
        
        sb.AppendLine($"- `{optionName}`");
        
        if (!string.IsNullOrEmpty(option.Description))
        {
            sb.AppendLine($"  - Description: {option.Description}");
        }
        
        // オプションの型情報
        if (option.ValueType != null && option.ValueType != typeof(bool))
        {
            var typeName = GetFriendlyTypeName(option.ValueType);
            sb.AppendLine($"  - Type: `{typeName}`");
        }
        
        // デフォルト値
        if (option.HasDefaultValue)
        {
            try
            {
                var defaultValue = option.GetDefaultValue();
                if (defaultValue != null)
                {
                    sb.AppendLine($"  - Default: `{defaultValue}`");
                }
            }
            catch
            {
                // デフォルト値の取得に失敗した場合は無視
            }
        }
        
        // 必須かどうか
        // Note: System.CommandLine doesn't expose IsRequired directly,
        // but we can check if it has a default value - options without defaults are typically required
        if (!option.HasDefaultValue && option.ValueType != typeof(bool))
        {
            sb.AppendLine("  - May be required depending on usage");
        }
        
        sb.AppendLine();
    }
    
    private static string GetFriendlyTypeName(Type type)
    {
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
        {
            return GetFriendlyTypeName(type.GetGenericArguments()[0]);
        }
        
        return type.Name switch
        {
            "String" => "string",
            "Int32" => "integer",
            "Boolean" => "boolean",
            "Double" => "decimal",
            _ => type.Name.ToLower()
        };
    }
    
    private static void AppendUsageExamples(StringBuilder sb)
    {
        sb.AppendLine("### Authentication");
        sb.AppendLine("```bash");
        sb.AppendLine("# Interactive login");
        sb.AppendLine("redmine auth login");
        sb.AppendLine();
        sb.AppendLine("# Login with options");
        sb.AppendLine("redmine auth login --url https://redmine.example.com --api-key YOUR_API_KEY --profile work");
        sb.AppendLine();
        sb.AppendLine("# Check authentication status");
        sb.AppendLine("redmine auth status");
        sb.AppendLine();
        sb.AppendLine("# Logout");
        sb.AppendLine("redmine auth logout");
        sb.AppendLine("```");
        sb.AppendLine();
        
        sb.AppendLine("### Issue Management");
        sb.AppendLine("```bash");
        sb.AppendLine("# List all issues assigned to me");
        sb.AppendLine("redmine issue list");
        sb.AppendLine();
        sb.AppendLine("# List issues with filters");
        sb.AppendLine("redmine issue list --assignee @me --status open --project myproject");
        sb.AppendLine();
        sb.AppendLine("# List with pagination");
        sb.AppendLine("redmine issue list --limit 50 --offset 100");
        sb.AppendLine();
        sb.AppendLine("# List with absolute time display");
        sb.AppendLine("redmine issue list --absolute-time");
        sb.AppendLine();
        sb.AppendLine("# View issue details");
        sb.AppendLine("redmine issue view 12345");
        sb.AppendLine();
        sb.AppendLine("# View in JSON format");
        sb.AppendLine("redmine issue view 12345 --json");
        sb.AppendLine();
        sb.AppendLine("# Open issue in web browser");
        sb.AppendLine("redmine issue view 12345 --web");
        sb.AppendLine();
        sb.AppendLine("# Create new issue interactively");
        sb.AppendLine("redmine issue create");
        sb.AppendLine();
        sb.AppendLine("# Create with options");
        sb.AppendLine("redmine issue create --title \"Bug fix\" --description \"Fixed null reference\" --project myproject");
        sb.AppendLine();
        sb.AppendLine("# Edit issue");
        sb.AppendLine("redmine issue edit 12345");
        sb.AppendLine();
        sb.AppendLine("# Add comment to issue");
        sb.AppendLine("redmine issue comment 12345 \"This has been resolved\"");
        sb.AppendLine("```");
        sb.AppendLine();
        
        sb.AppendLine("### Attachment Management");
        sb.AppendLine("```bash");
        sb.AppendLine("# Upload attachment");
        sb.AppendLine("redmine attachment upload file.pdf --description \"Design document\"");
        sb.AppendLine();
        sb.AppendLine("# List attachments for an issue");
        sb.AppendLine("redmine issue attachment 12345");
        sb.AppendLine();
        sb.AppendLine("# Download attachment");
        sb.AppendLine("redmine attachment download 98765");
        sb.AppendLine("```");
        sb.AppendLine();
        
        sb.AppendLine("### Configuration");
        sb.AppendLine("```bash");
        sb.AppendLine("# List all configuration");
        sb.AppendLine("redmine config list");
        sb.AppendLine();
        sb.AppendLine("# Get specific configuration");
        sb.AppendLine("redmine config get default_project");
        sb.AppendLine();
        sb.AppendLine("# Set configuration");
        sb.AppendLine("redmine config set default_project myproject");
        sb.AppendLine("```");
        sb.AppendLine();
        
        sb.AppendLine("### Special Syntax");
        sb.AppendLine();
        sb.AppendLine("- `@me` - Refers to the current authenticated user (e.g., `--assignee @me`)");
        sb.AppendLine("- Status values: `open`, `closed`, `all`, or specific status ID");
        sb.AppendLine("- Project can be specified by identifier or ID");
    }
}