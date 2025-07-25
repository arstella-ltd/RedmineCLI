using System.CommandLine;

using FluentAssertions;

using Microsoft.Extensions.Logging;

using NSubstitute;

using RedmineCLI.Commands;
using RedmineCLI.Tests.TestInfrastructure;

using Spectre.Console.Testing;

using Xunit;

namespace RedmineCLI.Tests.Commands;

[Collection("AnsiConsole")]
public class LlmsCommandTests
{
    private readonly ILogger<LlmsCommand> _logger;
    private readonly RootCommand _rootCommand;
    private readonly LlmsCommand _command;
    private readonly AnsiConsoleTestFixture _consoleFixture;

    public LlmsCommandTests()
    {
        _logger = Substitute.For<ILogger<LlmsCommand>>();
        _rootCommand = CreateTestRootCommand();
        _command = new LlmsCommand(_logger, _rootCommand);
        _consoleFixture = new AnsiConsoleTestFixture();
    }
    
    private RootCommand CreateTestRootCommand()
    {
        var rootCommand = new RootCommand("Test RedmineCLI");
        
        // Add test global options
        var licenseOption = new Option<bool>("--license") { Description = "Show license information" };
        rootCommand.Add(licenseOption);
        
        // Add test commands with options
        var authCommand = new Command("auth", "Authentication commands");
        var loginCommand = new Command("login", "Login to Redmine");
        loginCommand.Add(new Option<string>("--url") { Description = "Redmine server URL" });
        loginCommand.Add(new Option<string>("--api-key") { Description = "Redmine API key" });
        authCommand.Add(loginCommand);
        
        var issueCommand = new Command("issue", "Issue management commands");
        var listCommand = new Command("list", "List issues");
        
        var assigneeOption = new Option<string?>("--assignee") { Description = "Filter by assignee" };
        assigneeOption.Aliases.Add("-a");
        listCommand.Add(assigneeOption);
        
        var statusOption = new Option<string?>("--status") { Description = "Filter by status" };
        statusOption.Aliases.Add("-s");
        listCommand.Add(statusOption);
        
        var limitOption = new Option<int?>("--limit") { Description = "Limit number of results" };
        limitOption.Aliases.Add("-L");
        listCommand.Add(limitOption);
        
        listCommand.Add(new Option<int?>("--offset") { Description = "Offset for pagination" });
        listCommand.Add(new Option<bool>("--absolute-time") { Description = "Display absolute time instead of relative time" });
        listCommand.Add(new Option<bool>("--json") { Description = "Output in JSON format" });
        
        var webOption = new Option<bool>("--web") { Description = "Open in web browser" };
        webOption.Aliases.Add("-w");
        listCommand.Add(webOption);
        
        issueCommand.Add(listCommand);
        rootCommand.Add(authCommand);
        rootCommand.Add(issueCommand);
        
        return rootCommand;
    }

    [Fact]
    public async Task ShowLlmsInfoAsync_Should_ReturnSuccess()
    {
        // Act
        var result = await _command.ShowLlmsInfoAsync(CancellationToken.None);

        // Assert
        result.Should().Be(0);
    }

    [Fact]
    public void Create_Should_ReturnCommandWithCorrectName()
    {
        // Act
        var command = LlmsCommand.Create(_logger, _rootCommand);

        // Assert
        command.Name.Should().Be("llms");
        command.Description.Should().Be("Show LLM-friendly information about RedmineCLI");
    }

    [Fact]
    public async Task ShowLlmsInfoAsync_Should_OutputRedmineCLIInformation()
    {
        // Arrange & Act & Assert
        await _consoleFixture.ExecuteWithTestConsoleAsync(async console =>
        {
            var result = await _command.ShowLlmsInfoAsync(CancellationToken.None);

            result.Should().Be(0);
            var output = console.Output;
            output.Should().Contain("# RedmineCLI - Comprehensive Command Reference for LLMs");
            output.Should().Contain("## Installation");
            output.Should().Contain("## Authentication");
            output.Should().Contain("## Commands");
            output.Should().Contain("### `auth`");
            output.Should().Contain("### `issue`");
            output.Should().Contain("--offset");
            output.Should().Contain("--absolute-time");

            return result;
        });
    }

    [Fact]
    public async Task ShowLlmsInfoAsync_Should_IncludeAllEssentialSections()
    {
        // Arrange & Act & Assert
        await _consoleFixture.ExecuteWithTestConsoleAsync(async console =>
        {
            var result = await _command.ShowLlmsInfoAsync(CancellationToken.None);
            var output = console.Output;

            result.Should().Be(0);

            // 必須セクションの確認
            output.Should().Contain("## Installation");
            output.Should().Contain("## Authentication");
            output.Should().Contain("## Global Options");
            output.Should().Contain("## Commands");
            output.Should().Contain("## Configuration");
            output.Should().Contain("## Features");
            output.Should().Contain("## API Requirements");
            output.Should().Contain("## Example Workflows");
            
            // 全オプションが含まれていることを確認
            output.Should().Contain("--offset");
            output.Should().Contain("--absolute-time");
            output.Should().Contain("--assignee");
            output.Should().Contain("--status");
            output.Should().Contain("--limit");

            return result;
        });
    }

    [Fact]
    public async Task ShowLlmsInfoAsync_Should_IncludeImportantCommands()
    {
        // Arrange & Act & Assert
        await _consoleFixture.ExecuteWithTestConsoleAsync(async console =>
        {
            var result = await _command.ShowLlmsInfoAsync(CancellationToken.None);
            var output = console.Output;

            // 主要なコマンドが含まれていることを確認
            output.Should().Contain("### `auth`");
            output.Should().Contain("### `auth login`");
            output.Should().Contain("### `issue`");
            output.Should().Contain("### `issue list`");
            
            // オプションの詳細情報が含まれていることを確認
            output.Should().Contain("Description:");
            output.Should().Contain("Type:");
            output.Should().Contain("aliases:");

            return result;
        });
    }

    [Fact]
    public async Task ShowLlmsInfoAsync_Should_IncludeSpecialFeatures()
    {
        // Arrange & Act & Assert
        await _consoleFixture.ExecuteWithTestConsoleAsync(async console =>
        {
            var result = await _command.ShowLlmsInfoAsync(CancellationToken.None);
            var output = console.Output;

            // 特殊機能の説明が含まれていることを確認
            output.Should().Contain("Native AOT");
            output.Should().Contain("Sixel protocol");
            output.Should().Contain("--web");
            output.Should().Contain("table/json");
            
            // ページネーションオプションが含まれていることを確認
            output.Should().Contain("--offset");
            output.Should().Contain("Offset for pagination");
            output.Should().Contain("--limit");
            output.Should().Contain("Limit number of results");

            return result;
        });
    }
}
