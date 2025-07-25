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
    private readonly LlmsCommand _command;
    private readonly AnsiConsoleTestFixture _consoleFixture;

    public LlmsCommandTests()
    {
        _logger = Substitute.For<ILogger<LlmsCommand>>();
        _command = new LlmsCommand(_logger, null);
        _consoleFixture = new AnsiConsoleTestFixture();
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
        var command = LlmsCommand.Create(_logger, null);

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
            output.Should().Contain("## Core Commands");
            output.Should().Contain("redmine auth login");
            output.Should().Contain("redmine issue list");
            output.Should().Contain("redmine config set");

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
            output.Should().Contain("## Core Commands");
            output.Should().Contain("### Issue Management");
            output.Should().Contain("### Attachment Management");
            output.Should().Contain("### Configuration");
            output.Should().Contain("## All Available Options");
            output.Should().Contain("## Features");
            output.Should().Contain("## Configuration Files");
            output.Should().Contain("## API Requirements");
            output.Should().Contain("## Common Workflows");

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
            output.Should().Contain("redmine auth login");
            output.Should().Contain("redmine issue list");
            output.Should().Contain("redmine issue view");
            output.Should().Contain("redmine issue create");
            output.Should().Contain("redmine issue edit");
            output.Should().Contain("redmine issue comment");
            output.Should().Contain("redmine attachment download");
            output.Should().Contain("redmine config set");
            output.Should().Contain("redmine config get");
            output.Should().Contain("redmine config list");

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
            output.Should().Contain("@me");
            output.Should().Contain("Native AOT");
            output.Should().Contain("Sixel protocol");
            output.Should().Contain("--web");
            output.Should().Contain("--json");

            return result;
        });
    }

    [Fact]
    public async Task ShowLlmsInfoAsync_Should_IncludeAllOptions_When_RootCommandProvided()
    {
        // Arrange
        var rootCommand = new RootCommand("Test root command");
        var issueCommand = new Command("issue", "Manage issues");
        var listCommand = new Command("list", "List issues");

        // Add options that were missing in the static output
        var offsetOption = new Option<int?>("--offset") { Description = "Offset for pagination" };
        var absoluteTimeOption = new Option<bool>("--absolute-time") { Description = "Display absolute time instead of relative time" };

        listCommand.Add(offsetOption);
        listCommand.Add(absoluteTimeOption);
        issueCommand.Add(listCommand);
        rootCommand.Add(issueCommand);

        var commandWithRoot = new LlmsCommand(_logger, rootCommand);

        // Act & Assert
        await _consoleFixture.ExecuteWithTestConsoleAsync(async console =>
        {
            var result = await commandWithRoot.ShowLlmsInfoAsync(CancellationToken.None);
            var output = console.Output;

            result.Should().Be(0);

            // Verify that dynamically extracted options are included
            output.Should().Contain("--offset");
            output.Should().Contain("Offset for pagination");
            output.Should().Contain("--absolute-time");
            output.Should().Contain("Display absolute time instead of relative time");
            output.Should().Contain("issue list");

            return result;
        });
    }

    [Fact]
    public async Task ShowLlmsInfoAsync_Should_IncludeStaticOptions_When_RootCommandNotProvided()
    {
        // Arrange & Act & Assert
        await _consoleFixture.ExecuteWithTestConsoleAsync(async console =>
        {
            var result = await _command.ShowLlmsInfoAsync(CancellationToken.None);
            var output = console.Output;

            result.Should().Be(0);

            // Verify static fallback includes the missing options
            output.Should().Contain("--offset");
            output.Should().Contain("Offset for pagination");
            output.Should().Contain("--absolute-time");
            output.Should().Contain("Display absolute time instead of relative time");

            return result;
        });
    }
}
