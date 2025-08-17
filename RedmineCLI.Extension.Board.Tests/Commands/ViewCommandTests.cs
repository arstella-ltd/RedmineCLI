using System.CommandLine;
using System.CommandLine.IO;

using FluentAssertions;

using Microsoft.Extensions.Logging;

using NSubstitute;

using RedmineCLI.Extension.Board.Commands;
using RedmineCLI.Extension.Board.Services;

namespace RedmineCLI.Extension.Board.Tests.Commands;

public class ViewCommandTests
{
    private readonly ILogger<ViewCommand> _logger;
    private readonly IBoardService _boardService;
    private readonly IAuthenticationService _authenticationService;
    private readonly ViewCommand _viewCommand;

    public ViewCommandTests()
    {
        _logger = Substitute.For<ILogger<ViewCommand>>();
        _boardService = Substitute.For<IBoardService>();
        _authenticationService = Substitute.For<IAuthenticationService>();
        _viewCommand = new ViewCommand(_logger, _boardService, _authenticationService);
    }

    [Fact]
    public void Create_Should_ReturnCommand_With_CorrectName()
    {
        // Arrange
        // Act
        var command = _viewCommand.Create();

        // Assert
        command.Name.Should().Be("view");
    }

    [Fact]
    public void Create_Should_ReturnCommand_With_CorrectDescription()
    {
        // Arrange
        // Act
        var command = _viewCommand.Create();

        // Assert
        command.Description.Should().Be("View boards, topics, or topic details");
    }

    [Fact]
    public void Create_Should_ReturnCommand_With_TargetArgument()
    {
        // Arrange
        // Act
        var command = _viewCommand.Create();

        // Assert
        command.Arguments.Should().HaveCount(1);
        command.Arguments.First().Name.Should().Be("target");
    }

    [Fact]
    public void Create_Should_ReturnCommand_With_ProjectOption()
    {
        // Arrange
        // Act
        var command = _viewCommand.Create();

        // Assert
        command.Options.Should().Contain(o => o.Name == "project");
    }

    [Fact]
    public async Task HandleViewCommand_Should_ShowError_When_InvalidFormat()
    {
        // Arrange
        _authenticationService.GetAuthenticationAsync(null)
            .Returns(("https://example.com", "session"));
        var console = new TestConsole();
        var command = _viewCommand.Create();

        // Act
        var result = await command.InvokeAsync("view invalid:format:extra", console);

        // Assert
        result.Should().Be(0);
        await _boardService.DidNotReceive().ViewTopicAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<(string, string)>());
        await _boardService.DidNotReceive().ListTopicsAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<(string, string)>());
    }

    [Fact]
    public async Task HandleViewCommand_Should_Return_When_AuthenticationFails()
    {
        // Arrange
        _authenticationService.GetAuthenticationAsync(null)
            .Returns(("", ""));
        var console = new TestConsole();
        var command = _viewCommand.Create();

        // Act
        var result = await command.InvokeAsync("view 21", console);

        // Assert
        result.Should().Be(0);
        await _boardService.DidNotReceive().ListTopicsAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<(string, string)>());
    }

    [Fact]
    public async Task HandleViewCommand_Should_CallListTopics_When_BoardIdOnly()
    {
        // Arrange
        _authenticationService.GetAuthenticationAsync(null)
            .Returns(("https://example.com", "session"));
        var console = new TestConsole();
        var command = _viewCommand.Create();

        // Act
        var result = await command.InvokeAsync("view 21", console);

        // Assert
        result.Should().Be(0);
        await _boardService.Received(1).ListTopicsAsync(
            "21",
            null,
            ("session", "https://example.com"));
    }

    [Fact]
    public async Task HandleViewCommand_Should_CallViewTopic_When_BoardAndTopicId()
    {
        // Arrange
        _authenticationService.GetAuthenticationAsync(null)
            .Returns(("https://example.com", "session"));
        var console = new TestConsole();
        var command = _viewCommand.Create();

        // Act
        var result = await command.InvokeAsync("view 21:145", console);

        // Assert
        result.Should().Be(0);
        await _boardService.Received(1).ViewTopicAsync(
            "21",
            "145",
            null,
            ("session", "https://example.com"));
    }

    [Fact]
    public async Task HandleViewCommand_Should_CallViewTopic_With_ProjectOption()
    {
        // Arrange
        _authenticationService.GetAuthenticationAsync(null)
            .Returns(("https://example.com", "session"));
        var console = new TestConsole();
        var command = _viewCommand.Create();

        // Act
        var result = await command.InvokeAsync("view 21:145 --project myproject", console);

        // Assert
        result.Should().Be(0);
        await _boardService.Received(1).ViewTopicAsync(
            "21",
            "145",
            "myproject",
            ("session", "https://example.com"));
    }

    [Fact]
    public async Task HandleViewCommand_Should_ShowMessage_When_Wildcard()
    {
        // Arrange
        _authenticationService.GetAuthenticationAsync(null)
            .Returns(("https://example.com", "session"));
        var console = new TestConsole();
        var command = _viewCommand.Create();

        // Act
        var result = await command.InvokeAsync("view *", console);

        // Assert
        result.Should().Be(0);
        await _boardService.DidNotReceive().ViewTopicAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<(string, string)>());
        await _boardService.DidNotReceive().ListTopicsAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<(string, string)>());
    }
}
