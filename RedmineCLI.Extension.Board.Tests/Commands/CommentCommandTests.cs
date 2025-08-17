using System.CommandLine;
using System.CommandLine.IO;

using FluentAssertions;

using Microsoft.Extensions.Logging;

using NSubstitute;

using RedmineCLI.Extension.Board.Commands;
using RedmineCLI.Extension.Board.Services;

namespace RedmineCLI.Extension.Board.Tests.Commands;

public class CommentCommandTests
{
    private readonly ILogger<ReplyCommand> _replyLogger;
    private readonly IBoardService _boardService;
    private readonly IAuthenticationService _authenticationService;
    private readonly ReplyCommand _replyCommand;
    private readonly CommentCommand _commentCommand;

    public CommentCommandTests()
    {
        _replyLogger = Substitute.For<ILogger<ReplyCommand>>();
        _boardService = Substitute.For<IBoardService>();
        _authenticationService = Substitute.For<IAuthenticationService>();
        _replyCommand = new ReplyCommand(_replyLogger, _boardService, _authenticationService);
        _commentCommand = new CommentCommand(_replyCommand);
    }

    [Fact]
    public void Create_Should_ReturnCommand_With_CorrectName()
    {
        // Arrange
        // Act
        var command = _commentCommand.Create();

        // Assert
        command.Name.Should().Be("comment");
    }

    [Fact]
    public void Create_Should_ReturnCommand_With_CorrectDescription()
    {
        // Arrange
        // Act
        var command = _commentCommand.Create();

        // Assert
        command.Description.Should().Be("Comment on a board topic (alias for reply)");
    }

    [Fact]
    public void Create_Should_HaveSameArguments_As_ReplyCommand()
    {
        // Arrange
        var replyCommand = _replyCommand.Create();

        // Act
        var commentCommand = _commentCommand.Create();

        // Assert
        commentCommand.Arguments.Should().HaveCount(replyCommand.Arguments.Count);
        commentCommand.Arguments.First().Name.Should().Be(replyCommand.Arguments.First().Name);
    }

    [Fact]
    public void Create_Should_HaveSameOptions_As_ReplyCommand()
    {
        // Arrange
        var replyCommand = _replyCommand.Create();

        // Act
        var commentCommand = _commentCommand.Create();

        // Assert
        commentCommand.Options.Should().HaveCount(replyCommand.Options.Count);
        commentCommand.Options.Select(o => o.Name).Should().BeEquivalentTo(replyCommand.Options.Select(o => o.Name));
    }

    [Fact]
    public void Create_Should_HaveSameHandler_As_ReplyCommand()
    {
        // Arrange
        var replyCommand = _replyCommand.Create();

        // Act
        var commentCommand = _commentCommand.Create();

        // Assert
        commentCommand.Handler.Should().NotBeNull();
        commentCommand.Handler.GetType().Should().Be(replyCommand.Handler.GetType());
    }

    [Fact]
    public async Task CommentCommand_Should_BehaveIdentically_To_ReplyCommand_With_ValidInput()
    {
        // Arrange
        var console = new TestConsole();
        var commentCommand = _commentCommand.Create();
        var replyCommand = _replyCommand.Create();

        // Act
        var commentResult = await commentCommand.InvokeAsync("comment 21:145 -m \"test message\"", console);
        var replyResult = await replyCommand.InvokeAsync("reply 21:145 -m \"test message\"", console);

        // Assert
        commentResult.Should().Be(replyResult);
    }

    [Fact]
    public async Task CommentCommand_Should_BehaveIdentically_To_ReplyCommand_With_InvalidInput()
    {
        // Arrange
        var console = new TestConsole();
        var commentCommand = _commentCommand.Create();
        var replyCommand = _replyCommand.Create();

        // Act
        var commentResult = await commentCommand.InvokeAsync("comment invalid -m \"test message\"", console);
        var replyResult = await replyCommand.InvokeAsync("reply invalid -m \"test message\"", console);

        // Assert
        commentResult.Should().Be(replyResult);
    }

    [Fact]
    public async Task CommentCommand_Should_RequireMessageOption_Like_ReplyCommand()
    {
        // Arrange
        var console = new TestConsole();
        var commentCommand = _commentCommand.Create();

        // Act
        var result = await commentCommand.InvokeAsync("comment 21:145", console);

        // Assert
        // コマンドライン解析でエラーになるため、結果は0以外
        result.Should().NotBe(0);
    }

    [Fact]
    public void Create_Should_CopyAllArguments_From_ReplyCommand()
    {
        // Arrange
        var replyCommand = _replyCommand.Create();

        // Act
        var commentCommand = _commentCommand.Create();

        // Assert
        commentCommand.Arguments.Should().HaveCount(replyCommand.Arguments.Count);
        foreach (var replyArg in replyCommand.Arguments)
        {
            var commentArg = commentCommand.Arguments.FirstOrDefault(a => a.Name == replyArg.Name);
            commentArg.Should().NotBeNull();
            commentArg!.Name.Should().Be(replyArg.Name);
            commentArg.Description.Should().Be(replyArg.Description);
        }
    }

    [Fact]
    public void Create_Should_CopyAllOptions_From_ReplyCommand()
    {
        // Arrange
        var replyCommand = _replyCommand.Create();

        // Act
        var commentCommand = _commentCommand.Create();

        // Assert
        commentCommand.Options.Should().HaveCount(replyCommand.Options.Count);
        foreach (var replyOpt in replyCommand.Options)
        {
            var commentOpt = commentCommand.Options.FirstOrDefault(o => o.Name == replyOpt.Name);
            commentOpt.Should().NotBeNull();
            commentOpt!.Name.Should().Be(replyOpt.Name);
            commentOpt.IsRequired.Should().Be(replyOpt.IsRequired);
            commentOpt.Aliases.Should().BeEquivalentTo(replyOpt.Aliases);
        }
    }
}
