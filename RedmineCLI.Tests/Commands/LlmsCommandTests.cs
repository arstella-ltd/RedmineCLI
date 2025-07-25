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
        _command = new LlmsCommand(_logger);
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
        var command = LlmsCommand.Create(_logger);

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
            output.Should().Contain("# RedmineCLI");
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
            output.Should().Contain("## Options");
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
}
