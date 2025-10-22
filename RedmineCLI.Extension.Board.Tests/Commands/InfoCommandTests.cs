using System.CommandLine;
using System.CommandLine.Parsing;

using FluentAssertions;

using Microsoft.Extensions.Logging;

using NSubstitute;

using RedmineCLI.Extension.Board.Commands;

using Xunit;

namespace RedmineCLI.Extension.Board.Tests.Commands;

public class InfoCommandTests : IDisposable
{
    private readonly ILogger<InfoCommand> _mockLogger;
    private readonly InfoCommand _command;
    private readonly StringWriter _consoleOutput;
    private readonly TextWriter _originalOutput;

    public InfoCommandTests()
    {
        _mockLogger = Substitute.For<ILogger<InfoCommand>>();
        _command = new InfoCommand(_mockLogger);

        // Capture console output
        _originalOutput = Console.Out;
        _consoleOutput = new StringWriter();
        Console.SetOut(_consoleOutput);
    }

    public void Dispose()
    {
        // Restore original console output
        Console.SetOut(_originalOutput);
        _consoleOutput.Dispose();
    }

    [Fact]
    public void Create_Should_ReturnCommandWithCorrectName()
    {
        // Act
        var command = _command.Create();

        // Assert
        command.Name.Should().Be("info");
    }

    [Fact]
    public void Create_Should_ReturnCommandWithCorrectDescription()
    {
        // Act
        var command = _command.Create();

        // Assert
        command.Description.Should().Be("Display extension and environment information");
    }

    [Fact]
    public async Task Command_Should_DisplayBasicInfo()
    {
        // Arrange
        var command = _command.Create();
        var parseResult = command.Parse("info");

        // Act
        await parseResult.InvokeAsync();
        var output = _consoleOutput.ToString();

        // Assert
        output.Should().Contain("RedmineCLI Board Extension v1.0.0");
        output.Should().Contain("This extension provides board management functionality for RedmineCLI");
        output.Should().Contain("Available commands:");
        output.Should().Contain("list     - List all boards");
        output.Should().Contain("info     - Display this information");
    }

    [Fact]
    public async Task Command_Should_DisplayEnvironmentVariables()
    {
        // Arrange
        var command = _command.Create();
        var parseResult = command.Parse("info");

        // Act
        await parseResult.InvokeAsync();
        var output = _consoleOutput.ToString();

        // Assert
        output.Should().Contain("Environment variables:");
        output.Should().Contain("REDMINE_URL:");
        output.Should().Contain("REDMINE_API_KEY:");
        output.Should().Contain("REDMINE_USER:");
        output.Should().Contain("REDMINE_PROJECT:");
        output.Should().Contain("REDMINE_CONFIG_DIR:");
    }

    [Fact]
    public async Task Command_Should_MaskApiKey_When_Set()
    {
        // Arrange
        Environment.SetEnvironmentVariable("REDMINE_API_KEY", "mysecretapikey");
        var command = _command.Create();
        var parseResult = command.Parse("info");

        try
        {
            // Act
            await parseResult.InvokeAsync();
            var output = _consoleOutput.ToString();

            // Assert
            output.Should().Contain("REDMINE_API_KEY: myse...");
            output.Should().NotContain("mysecretapikey");
        }
        finally
        {
            // Clean up
            Environment.SetEnvironmentVariable("REDMINE_API_KEY", null);
        }
    }

    [Fact]
    public async Task Command_Should_ShowNotSet_When_EnvironmentVariableNotSet()
    {
        // Arrange
        Environment.SetEnvironmentVariable("REDMINE_URL", null);
        var command = _command.Create();
        var parseResult = command.Parse("info");

        // Act
        await parseResult.InvokeAsync();
        var output = _consoleOutput.ToString();

        // Assert
        output.Should().Contain("REDMINE_URL: (not set)");
    }

    [Fact]
    public async Task Command_Should_ShowEnvironmentVariableValue_When_Set()
    {
        // Arrange
        Environment.SetEnvironmentVariable("REDMINE_URL", "https://redmine.example.com");
        var command = _command.Create();
        var parseResult = command.Parse("info");

        try
        {
            // Act
            await parseResult.InvokeAsync();
            var output = _consoleOutput.ToString();

            // Assert
            output.Should().Contain("REDMINE_URL: https://redmine.example.com");
        }
        finally
        {
            // Clean up
            Environment.SetEnvironmentVariable("REDMINE_URL", null);
        }
    }

    [Fact]
    public async Task Command_Should_MaskShortApiKey()
    {
        // Arrange
        Environment.SetEnvironmentVariable("REDMINE_API_KEY", "abc");
        var command = _command.Create();
        var parseResult = command.Parse("info");

        try
        {
            // Act
            await parseResult.InvokeAsync();
            var output = _consoleOutput.ToString();

            // Assert
            output.Should().Contain("REDMINE_API_KEY: ***");
        }
        finally
        {
            // Clean up
            Environment.SetEnvironmentVariable("REDMINE_API_KEY", null);
        }
    }
}
