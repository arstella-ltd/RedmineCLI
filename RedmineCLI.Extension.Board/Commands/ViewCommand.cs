using System.CommandLine;

using Microsoft.Extensions.Logging;

using RedmineCLI.Extension.Board.Parsers;
using RedmineCLI.Extension.Board.Services;

namespace RedmineCLI.Extension.Board.Commands;

/// <summary>
/// redmine-board view コマンドの実装
/// </summary>
public class ViewCommand
{
    private readonly ILogger<ViewCommand> _logger;
    private readonly IBoardService _boardService;
    private readonly IAuthenticationService _authenticationService;

    public ViewCommand(
        ILogger<ViewCommand> logger,
        IBoardService boardService,
        IAuthenticationService authenticationService)
    {
        _logger = logger;
        _boardService = boardService;
        _authenticationService = authenticationService;
    }

    public Command Create()
    {
        var command = new Command("view", "View boards, topics, or topic details");

        // board:topic形式の引数
        var targetArgument = new Argument<string>(
            "target",
            "Board ID, board:topic notation (e.g., 21:145), or * for all boards");
        command.Add(targetArgument);

        // プロジェクトオプション
        var projectOption = new Option<string>(
            "--project",
            "Project name or ID");
        command.Add(projectOption);

        command.SetHandler(async (string target, string? project) =>
        {
            await HandleViewCommand(target, project);
        }, targetArgument, projectOption);

        return command;
    }

    private async Task HandleViewCommand(string target, string? project)
    {
        var parseResult = BoardTopicParser.Parse(target);

        if (!parseResult.IsValid)
        {
            Spectre.Console.AnsiConsole.MarkupLine($"[red]Invalid format: '{target}'[/]");
            Spectre.Console.AnsiConsole.MarkupLine("Usage: redmine-board view <board-id> | <board-id>:<topic-id> | *");
            return;
        }

        var (url, sessionCookie) = await _authenticationService.GetAuthenticationAsync(null);
        if (string.IsNullOrEmpty(sessionCookie))
        {
            return;
        }

        var auth = (sessionCookie, url);

        if (parseResult.IsWildcard)
        {
            // 全ボードのトピック一覧（将来実装）
            Spectre.Console.AnsiConsole.MarkupLine("[yellow]Wildcard (*) support is not yet implemented.[/]");
        }
        else if (parseResult.TopicId.HasValue)
        {
            // トピック詳細と返信を表示
            await _boardService.ViewTopicAsync(
                parseResult.BoardId!.Value.ToString(),
                parseResult.TopicId.Value.ToString(),
                project,
                auth);
        }
        else if (parseResult.BoardId.HasValue)
        {
            // ボードのトピック一覧を表示
            await _boardService.ListTopicsAsync(
                parseResult.BoardId.Value.ToString(),
                project,
                auth);
        }
    }
}
