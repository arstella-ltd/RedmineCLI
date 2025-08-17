using System.CommandLine;
using System.CommandLine.Parsing;

using FluentAssertions;

using Microsoft.Extensions.Logging;

using NSubstitute;

using RedmineCLI.Extension.Board.Commands;
using RedmineCLI.Extension.Board.Services;

using Xunit;

namespace RedmineCLI.Extension.Board.Tests.Commands;

public class BoardTopicCommandTests
{
    private readonly ILogger<BoardTopicCommand> _mockLogger;
    private readonly IBoardService _mockBoardService;
    private readonly IAuthenticationService _mockAuthenticationService;
    private readonly BoardTopicCommand _command;

    public BoardTopicCommandTests()
    {
        _mockLogger = Substitute.For<ILogger<BoardTopicCommand>>();
        _mockBoardService = Substitute.For<IBoardService>();
        _mockAuthenticationService = Substitute.For<IAuthenticationService>();
        _command = new BoardTopicCommand(_mockLogger, _mockBoardService, _mockAuthenticationService);
    }

    [Fact]
    public void CreateDynamicBoardCommand_Should_ReturnNull_When_NoArgs()
    {
        // Arrange
        var args = Array.Empty<string>();

        // Act
        var result = _command.CreateDynamicBoardCommand(args);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void CreateDynamicBoardCommand_Should_ReturnNull_When_FirstArgNotNumber()
    {
        // Arrange
        var args = new[] { "not-a-number", "topic", "list" };

        // Act
        var result = _command.CreateDynamicBoardCommand(args);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void CreateDynamicBoardCommand_Should_CreateCommand_When_FirstArgIsNumber()
    {
        // Arrange
        var args = new[] { "123" };

        // Act
        var result = _command.CreateDynamicBoardCommand(args);

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("123");
        result.Description.Should().Be("Operations for board 123");
        result.IsHidden.Should().BeTrue();
    }

    [Fact]
    public void CreateDynamicBoardCommand_Should_HaveTopicSubcommand()
    {
        // Arrange
        var args = new[] { "123" };

        // Act
        var result = _command.CreateDynamicBoardCommand(args);

        // Assert
        result.Should().NotBeNull();
        var topicCommand = result!.Subcommands.FirstOrDefault(c => c.Name == "topic");
        topicCommand.Should().NotBeNull();
        topicCommand!.Description.Should().Be("Topic operations");
    }

    [Fact]
    public void CreateDynamicBoardCommand_Should_HaveTopicListSubcommand()
    {
        // Arrange
        var args = new[] { "123" };

        // Act
        var result = _command.CreateDynamicBoardCommand(args);

        // Assert
        result.Should().NotBeNull();
        var topicCommand = result!.Subcommands.FirstOrDefault(c => c.Name == "topic");
        topicCommand.Should().NotBeNull();
        var listCommand = topicCommand!.Subcommands.FirstOrDefault(c => c.Name == "list");
        listCommand.Should().NotBeNull();
        listCommand!.Description.Should().Be("List topics in the board");
        listCommand.Aliases.Should().Contain("ls");
    }

    [Fact]
    public void CreateDynamicBoardCommand_Should_HaveProjectOption_In_TopicList()
    {
        // Arrange
        var args = new[] { "123" };

        // Act
        var result = _command.CreateDynamicBoardCommand(args);

        // Assert
        result.Should().NotBeNull();
        var topicCommand = result!.Subcommands.FirstOrDefault(c => c.Name == "topic");
        var listCommand = topicCommand!.Subcommands.FirstOrDefault(c => c.Name == "list");
        var projectOption = listCommand!.Options.FirstOrDefault(o => o.Name == "project");
        projectOption.Should().NotBeNull();
        projectOption!.Description.Should().Be("Project name or ID");
    }

    [Fact]
    public async Task TopicList_Should_CallBoardService_WithCorrectParameters()
    {
        // Arrange
        var args = new[] { "123" };
        _mockAuthenticationService.GetAuthenticationAsync(null)
            .Returns(Task.FromResult<(string, string?)>(("https://redmine.example.com", "session-cookie")));

        var command = _command.CreateDynamicBoardCommand(args);
        var parseResult = command!.Parse("123 topic list --project test-project");

        // Act
        await parseResult.InvokeAsync();

        // Assert
        await _mockBoardService.Received(1).ListTopicsAsync("123", "test-project", ("session-cookie", "https://redmine.example.com"));
    }

    [Fact]
    public async Task TopicList_Should_NotCallBoardService_When_NoSessionCookie()
    {
        // Arrange
        var args = new[] { "123" };
        _mockAuthenticationService.GetAuthenticationAsync(null)
            .Returns(Task.FromResult<(string, string?)>(("https://redmine.example.com", null)));

        var command = _command.CreateDynamicBoardCommand(args);
        var parseResult = command!.Parse("123 topic list");

        // Act
        await parseResult.InvokeAsync();

        // Assert
        await _mockBoardService.DidNotReceive().ListTopicsAsync(Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<(string, string)>());
    }

    [Fact]
    public async Task TopicView_Should_CallBoardService_WithCorrectParameters()
    {
        // Arrange
        var args = new[] { "123" };
        _mockAuthenticationService.GetAuthenticationAsync(null)
            .Returns(Task.FromResult<(string, string?)>(("https://redmine.example.com", "session-cookie")));

        var command = _command.CreateDynamicBoardCommand(args);
        var parseResult = command!.Parse("123 topic 456 --project test-project");

        // Act
        await parseResult.InvokeAsync();

        // Assert
        await _mockBoardService.Received(1).ViewTopicAsync("123", "456", "test-project", ("session-cookie", "https://redmine.example.com"));
    }

    [Fact]
    public void CreateDynamicBoardCommand_Should_HaveTopicIdArgument()
    {
        // Arrange
        var args = new[] { "123" };

        // Act
        var result = _command.CreateDynamicBoardCommand(args);

        // Assert
        result.Should().NotBeNull();
        var topicCommand = result!.Subcommands.FirstOrDefault(c => c.Name == "topic");
        topicCommand.Should().NotBeNull();
        var topicIdArg = topicCommand!.Arguments.FirstOrDefault(a => a.Name == "topic-id");
        topicIdArg.Should().NotBeNull();
        topicIdArg!.Description.Should().Be("Topic ID");
    }

    [Fact]
    public void CreateDynamicBoardCommand_Should_HaveProjectOption_In_TopicView()
    {
        // Arrange
        var args = new[] { "123" };

        // Act
        var result = _command.CreateDynamicBoardCommand(args);

        // Assert
        result.Should().NotBeNull();
        var topicCommand = result!.Subcommands.FirstOrDefault(c => c.Name == "topic");
        var projectOption = topicCommand!.Options.FirstOrDefault(o => o.Name == "project");
        projectOption.Should().NotBeNull();
        projectOption!.Description.Should().Be("Project name or ID");
    }
}
