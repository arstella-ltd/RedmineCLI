using System.CommandLine;
using System.CommandLine.Binding;
using System.Text;

using Microsoft.Extensions.Logging;

using Spectre.Console;

namespace RedmineCLI.Commands;

public class LlmsCommand
{
    private readonly ILogger<LlmsCommand> _logger;
    private readonly RootCommand _rootCommand;

    public LlmsCommand(ILogger<LlmsCommand> logger, RootCommand rootCommand)
    {
        _logger = logger;
        _rootCommand = rootCommand;
    }

    public static Command Create(ILogger<LlmsCommand> logger, RootCommand rootCommand)
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

            AnsiConsole.WriteLine("# RedmineCLI - Comprehensive Command Reference for LLMs");
            AnsiConsole.WriteLine();
            AnsiConsole.WriteLine("This document provides a comprehensive reference of all RedmineCLI commands and options for LLM usage.");
            AnsiConsole.WriteLine();
            
            AnsiConsole.WriteLine("## Overview");
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
            
            // Extract and display global options
            AnsiConsole.WriteLine("## Global Options");
            AnsiConsole.WriteLine();
            AnsiConsole.WriteLine("These options are available for all commands:");
            AnsiConsole.WriteLine();
            
            DisplayOptions(_rootCommand.Options);
            AnsiConsole.WriteLine();
            
            // Extract and display all commands with their options
            AnsiConsole.WriteLine("## Commands");
            AnsiConsole.WriteLine();
            
            foreach (var command in _rootCommand.Children.OfType<Command>().OrderBy(c => c.Name))
            {
                DisplayCommand(command);
            }
            
            AnsiConsole.WriteLine("## Configuration");
            AnsiConsole.WriteLine();
            AnsiConsole.WriteLine("Configuration files are stored in:");
            AnsiConsole.WriteLine("- Windows: `%APPDATA%\\redmine\\config.yml`");
            AnsiConsole.WriteLine("- macOS/Linux: `~/.config/redmine/config.yml`");
            AnsiConsole.WriteLine();
            
            AnsiConsole.WriteLine("### Configuration Keys");
            AnsiConsole.WriteLine("- `time.format`: Time display format (relative/absolute)");
            AnsiConsole.WriteLine("- `defaultformat`: Default output format (table/json)");
            AnsiConsole.WriteLine("- `editor`: Preferred text editor");
            AnsiConsole.WriteLine();
            
            AnsiConsole.WriteLine("## Features");
            AnsiConsole.WriteLine();
            AnsiConsole.WriteLine("- Native AOT compiled for fast startup (<100ms)");
            AnsiConsole.WriteLine("- Cross-platform (Windows, macOS, Linux)");
            AnsiConsole.WriteLine("- Multiple profile support");
            AnsiConsole.WriteLine("- Sixel protocol support for inline image display");
            AnsiConsole.WriteLine("- Table and JSON output formats");
            AnsiConsole.WriteLine("- Editor integration for comments");
            AnsiConsole.WriteLine("- Interactive and non-interactive modes");
            AnsiConsole.WriteLine();
            
            AnsiConsole.WriteLine("## API Requirements");
            AnsiConsole.WriteLine();
            AnsiConsole.WriteLine("- Redmine REST API v3.0 or higher");
            AnsiConsole.WriteLine("- API key authentication");
            AnsiConsole.WriteLine();
            
            AnsiConsole.WriteLine("## Example Workflows");
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
            AnsiConsole.WriteLine("### List issues with pagination");
            AnsiConsole.WriteLine("```bash");
            AnsiConsole.WriteLine("redmine issue list --limit 50 --offset 100");
            AnsiConsole.WriteLine("```");
            AnsiConsole.WriteLine();
            AnsiConsole.WriteLine("### View issue with absolute time");
            AnsiConsole.WriteLine("```bash");
            AnsiConsole.WriteLine("redmine issue view 12345 --absolute-time");
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
    
    private void DisplayCommand(Command command, string prefix = "")
    {
        var fullCommandName = string.IsNullOrEmpty(prefix) ? command.Name : $"{prefix} {command.Name}";
        
        AnsiConsole.WriteLine($"### `{fullCommandName}`");
        AnsiConsole.WriteLine();
        
        if (!string.IsNullOrEmpty(command.Description))
        {
            AnsiConsole.WriteLine($"**Description**: {command.Description}");
            AnsiConsole.WriteLine();
        }
        
        // Display arguments
        var arguments = command.Arguments;
        if (arguments.Any())
        {
            AnsiConsole.WriteLine("**Arguments**:");
            AnsiConsole.WriteLine();
            foreach (var arg in arguments)
            {
                AnsiConsole.WriteLine($"- `{arg.Name}`");
                if (!string.IsNullOrEmpty(arg.Description))
                {
                    AnsiConsole.WriteLine($"  - Description: {arg.Description}");
                }
                if (arg.Arity.MaximumNumberOfValues > 1)
                {
                    AnsiConsole.WriteLine($"  - Can accept multiple values");
                }
                AnsiConsole.WriteLine();
            }
        }
        
        // Display options
        var options = command.Options;
        if (options.Any())
        {
            AnsiConsole.WriteLine("**Options**:");
            AnsiConsole.WriteLine();
            DisplayOptions(options);
        }
        
        // Display subcommands
        var subcommands = command.Children.OfType<Command>().OrderBy(c => c.Name);
        if (subcommands.Any())
        {
            AnsiConsole.WriteLine("**Subcommands**:");
            AnsiConsole.WriteLine();
            foreach (var subcommand in subcommands)
            {
                DisplayCommand(subcommand, fullCommandName);
            }
        }
        
        AnsiConsole.WriteLine();
    }
    
    private void DisplayOptions(IEnumerable<Option> options)
    {
        foreach (var option in options.OrderBy(o => o.Name))
        {
            var sb = new StringBuilder();
            sb.Append($"- `{option.Name}`");
            
            // Add aliases
            if (option.Aliases.Count > 1) // First alias is the primary name
            {
                var aliases = option.Aliases.Skip(1).ToList();
                if (aliases.Any())
                {
                    sb.Append($" (aliases: {string.Join(", ", aliases.Select(a => $"`{a}`"))})");
                }
            }
            
            AnsiConsole.WriteLine(sb.ToString());
            
            // Description
            if (!string.IsNullOrEmpty(option.Description))
            {
                AnsiConsole.WriteLine($"  - Description: {option.Description}");
            }
            
            // Type
            var optionType = option.ValueType;
            if (optionType != null)
            {
                var typeName = GetFriendlyTypeName(optionType);
                AnsiConsole.WriteLine($"  - Type: `{typeName}`");
            }
            
            // Since reflection is problematic with AOT, we'll skip default values and required status
            // These would need to be manually documented or extracted differently
            
            AnsiConsole.WriteLine();
        }
    }
    
    private string GetFriendlyTypeName(Type type)
    {
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
        {
            return GetFriendlyTypeName(type.GetGenericArguments()[0]) + " (optional)";
        }
        
        return type.Name switch
        {
            "String" => "string",
            "Int32" => "integer",
            "Boolean" => "boolean",
            "DateTime" => "datetime",
            _ => type.Name.ToLowerInvariant()
        };
    }
}