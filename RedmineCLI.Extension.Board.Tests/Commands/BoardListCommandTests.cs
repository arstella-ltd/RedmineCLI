using System.CommandLine;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;

using FluentAssertions;

using Microsoft.Extensions.Logging;

using NSubstitute;

using RedmineCLI.Extension.Board.Commands;
using RedmineCLI.Extension.Board.Services;

using Xunit;

namespace RedmineCLI.Extension.Board.Tests.Commands;

public class BoardListCommandTests
{
    private readonly ILogger<BoardListCommand> _mockLogger;
    private readonly IBoardService _mockBoardService;
    private readonly BoardListCommand _command;

    public BoardListCommandTests()
    {
        _mockLogger = Substitute.For<ILogger<BoardListCommand>>();
        _mockBoardService = Substitute.For<IBoardService>();
        _command = new BoardListCommand(_mockLogger, _mockBoardService);
    }

    [Fact]
    public void Create_Should_ReturnCommandWithCorrectName()
    {
        // Act
        var command = _command.Create();

        // Assert
        command.Name.Should().Be("list");
    }

    [Fact]
    public void Create_Should_ReturnCommandWithCorrectDescription()
    {
        // Act
        var command = _command.Create();

        // Assert
        command.Description.Should().Be("List all boards (requires 'redmine auth login' first)");
    }

    [Fact]
    public void Create_Should_AddAliasLs()
    {
        // Act
        var command = _command.Create();

        // Assert
        command.Aliases.Should().Contain("ls");
    }

    [Fact]
    public void Create_Should_AddProjectOption()
    {
        // Act
        var command = _command.Create();

        // Assert
        var projectOption = command.Options.FirstOrDefault(o => o.Name == "project");
        projectOption.Should().NotBeNull();
        projectOption!.Description.Should().Be("Filter by project name or ID");
    }

    [Fact]
    public void Create_Should_AddUrlOption()
    {
        // Act
        var command = _command.Create();

        // Assert
        var urlOption = command.Options.FirstOrDefault(o => o.Name == "url");
        urlOption.Should().NotBeNull();
        urlOption!.Description.Should().Be("Redmine server URL (optional, uses stored credentials by default)");
    }

    [Fact]
    public async Task Command_Should_CallBoardService_WithCorrectParameters()
    {
        // Arrange
        var command = _command.Create();
        var parseResult = command.Parse("list --project test-project --url https://redmine.example.com");

        // Act
        await parseResult.InvokeAsync();

        // Assert
        await _mockBoardService.Received(1).ListBoardsAsync("test-project", "https://redmine.example.com");
    }

    [Fact]
    public async Task Command_Should_CallBoardService_WithNullParameters_When_OptionsNotProvided()
    {
        // Arrange
        var command = _command.Create();
        var parseResult = command.Parse("list");

        // Act
        await parseResult.InvokeAsync();

        // Assert
        await _mockBoardService.Received(1).ListBoardsAsync(null, null);
    }

    [Fact]
    public async Task Command_Should_CallBoardService_WithOnlyProject()
    {
        // Arrange
        var command = _command.Create();
        var parseResult = command.Parse("list --project my-project");

        // Act
        await parseResult.InvokeAsync();

        // Assert
        await _mockBoardService.Received(1).ListBoardsAsync("my-project", null);
    }

    [Fact]
    public async Task Command_Should_CallBoardService_WithOnlyUrl()
    {
        // Arrange
        var command = _command.Create();
        var parseResult = command.Parse("list --url https://custom.redmine.com");

        // Act
        await parseResult.InvokeAsync();

        // Assert
        await _mockBoardService.Received(1).ListBoardsAsync(null, "https://custom.redmine.com");
    }

    [Fact]
    public async Task Command_Should_Work_WithAlias()
    {
        // Arrange
        var command = _command.Create();
        var parseResult = command.Parse("ls --project test");

        // Act
        await parseResult.InvokeAsync();

        // Assert
        await _mockBoardService.Received(1).ListBoardsAsync("test", null);
    }
}
