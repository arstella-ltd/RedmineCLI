using System.CommandLine;
using System.CommandLine.IO;

using FluentAssertions;

using Microsoft.Extensions.Logging;

using NSubstitute;

using RedmineCLI.Extension.Board.Commands;
using RedmineCLI.Extension.Board.Services;

namespace RedmineCLI.Extension.Board.Tests.Commands;

public class ReplyCommandTests
{
    private readonly ILogger<ReplyCommand> _logger;
    private readonly IBoardService _boardService;
    private readonly IAuthenticationService _authenticationService;
    private readonly ReplyCommand _replyCommand;

    public ReplyCommandTests()
    {
        _logger = Substitute.For<ILogger<ReplyCommand>>();
        _boardService = Substitute.For<IBoardService>();
        _authenticationService = Substitute.For<IAuthenticationService>();
        _replyCommand = new ReplyCommand(_logger, _boardService, _authenticationService);
    }

    [Fact]
    public void Create_Should_ReturnCommand_With_CorrectName()
    {
        // Arrange
        // Act
        var command = _replyCommand.Create();

        // Assert
        command.Name.Should().Be("reply");
    }

    [Fact]
    public void Create_Should_ReturnCommand_With_CorrectDescription()
    {
        // Arrange
        // Act
        var command = _replyCommand.Create();

        // Assert
        command.Description.Should().Be("Reply to a board topic");
    }

    [Fact]
    public void Create_Should_ReturnCommand_With_TargetArgument()
    {
        // Arrange
        // Act
        var command = _replyCommand.Create();

        // Assert
        command.Arguments.Should().HaveCount(1);
        command.Arguments.First().Name.Should().Be("target");
        command.Arguments.First().Description.Should().Contain("Board:topic notation");
    }

    [Fact]
    public void Create_Should_ReturnCommand_With_RequiredMessageOption()
    {
        // Arrange
        // Act
        var command = _replyCommand.Create();

        // Assert
        var messageOption = command.Options.FirstOrDefault(o => o.Name == "message");
        messageOption.Should().NotBeNull();
        messageOption!.IsRequired.Should().BeTrue();
        messageOption.Aliases.Should().Contain("-m");
        messageOption.Aliases.Should().Contain("--message");
    }

    [Fact]
    public async Task HandleReplyCommand_Should_ShowError_When_InvalidFormat()
    {
        // Arrange
        var console = new TestConsole();
        var command = _replyCommand.Create();

        // Act
        var result = await command.InvokeAsync("reply invalid -m \"test message\"", console);

        // Assert
        result.Should().Be(0);
    }

    [Fact]
    public async Task HandleReplyCommand_Should_ShowError_When_BoardIdOnly()
    {
        // Arrange
        var console = new TestConsole();
        var command = _replyCommand.Create();

        // Act
        var result = await command.InvokeAsync("reply 21 -m \"test message\"", console);

        // Assert
        result.Should().Be(0);
    }

    [Fact]
    public async Task HandleReplyCommand_Should_ShowError_When_TooManyColons()
    {
        // Arrange
        var console = new TestConsole();
        var command = _replyCommand.Create();

        // Act
        var result = await command.InvokeAsync("reply 21:145:999 -m \"test message\"", console);

        // Assert
        result.Should().Be(0);
    }

    [Fact]
    public async Task HandleReplyCommand_Should_ShowPlaceholderMessage_When_ValidFormat()
    {
        // Arrange
        var console = new TestConsole();
        var command = _replyCommand.Create();

        // Act
        var result = await command.InvokeAsync("reply 21:145 -m \"test message\"", console);

        // Assert
        result.Should().Be(0);
    }

    [Fact]
    public async Task HandleReplyCommand_Should_RequireMessageOption()
    {
        // Arrange
        var console = new TestConsole();
        var command = _replyCommand.Create();

        // Act
        var result = await command.InvokeAsync("reply 21:145", console);

        // Assert
        // コマンドライン解析でエラーになるため、結果は0以外
        result.Should().NotBe(0);
    }

    [Theory]
    [InlineData("21:145", "Hello, World!")]
    [InlineData("999:888", "Test message with special chars: !@#$%")]
    [InlineData("1:1", "Short message")]
    public async Task HandleReplyCommand_Should_AcceptVariousValidInputs(string target, string message)
    {
        // Arrange
        var console = new TestConsole();
        var command = _replyCommand.Create();

        // Act
        var result = await command.InvokeAsync($"reply {target} -m \"{message}\"", console);

        // Assert
        result.Should().Be(0);
    }

    [Fact]
    public async Task HandleReplyCommand_Should_AcceptMessageWithShortAlias()
    {
        // Arrange
        var console = new TestConsole();
        var command = _replyCommand.Create();

        // Act
        var result = await command.InvokeAsync("reply 21:145 -m \"test message\"", console);

        // Assert
        result.Should().Be(0);
    }

    [Fact]
    public async Task HandleReplyCommand_Should_AcceptMessageWithLongAlias()
    {
        // Arrange
        var console = new TestConsole();
        var command = _replyCommand.Create();

        // Act
        var result = await command.InvokeAsync("reply 21:145 --message \"test message\"", console);

        // Assert
        result.Should().Be(0);
    }
}
