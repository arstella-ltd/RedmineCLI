using System.CommandLine;

using Microsoft.Extensions.Logging;

using RedmineCLI.Extension.Board.Parsers;
using RedmineCLI.Extension.Board.Services;

namespace RedmineCLI.Extension.Board.Commands;

/// <summary>
/// redmine-board reply コマンドの実装
/// </summary>
public class ReplyCommand
{
    private readonly ILogger<ReplyCommand> _logger;
    private readonly IBoardService _boardService;
    private readonly IAuthenticationService _authenticationService;

    public ReplyCommand(
        ILogger<ReplyCommand> logger,
        IBoardService boardService,
        IAuthenticationService authenticationService)
    {
        _logger = logger;
        _boardService = boardService;
        _authenticationService = authenticationService;
    }

    public Command Create()
    {
        var command = new Command("reply", "Reply to a board topic");

        // board:topic形式の引数
        var targetArgument = new Argument<string>(
            "target",
            "Board:topic notation (e.g., 21:145)");
        command.Add(targetArgument);

        // メッセージオプション
        var messageOption = new Option<string>(
            new[] { "-m", "--message" },
            "Reply message")
        {
            IsRequired = true
        };
        command.Add(messageOption);

        command.SetHandler(async (string target, string message) =>
        {
            await HandleReplyCommand(target, message);
        }, targetArgument, messageOption);

        return command;
    }

    private Task HandleReplyCommand(string target, string message)
    {
        var parseResult = BoardTopicParser.Parse(target);

        if (!parseResult.IsValid || !parseResult.TopicId.HasValue)
        {
            Spectre.Console.AnsiConsole.MarkupLine($"[red]Invalid format: '{target}'[/]");
            Spectre.Console.AnsiConsole.MarkupLine("Usage: redmine-board reply <board-id>:<topic-id> -m <message>");
            return Task.CompletedTask;
        }

        // 現時点では返信機能は未実装のため、プレースホルダーメッセージを表示
        Spectre.Console.AnsiConsole.MarkupLine($"[yellow]Reply functionality to topic {parseResult.BoardId}:{parseResult.TopicId} is not yet implemented.[/]");
        Spectre.Console.AnsiConsole.MarkupLine($"[dim]Message: {message}[/]");
        return Task.CompletedTask;
    }
}
