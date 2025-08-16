using System.CommandLine;

using Microsoft.Extensions.Logging;

using RedmineCLI.Extension.Board.Services;

namespace RedmineCLI.Extension.Board.Commands;

/// <summary>
/// ボード一覧を表示するコマンド
/// </summary>
public class BoardListCommand
{
    private readonly ILogger<BoardListCommand> _logger;
    private readonly IBoardService _boardService;

    public BoardListCommand(ILogger<BoardListCommand> logger, IBoardService boardService)
    {
        _logger = logger;
        _boardService = boardService;
    }

    public Command Create()
    {
        var command = new Command("list", "List all boards (requires 'redmine auth login' first)");
        command.AddAlias("ls");

        var projectOption = new Option<string>("--project", "Filter by project name or ID");
        var urlOption = new Option<string>("--url", "Redmine server URL (optional, uses stored credentials by default)");

        command.Add(projectOption);
        command.Add(urlOption);

        command.SetHandler(async (string? project, string? url) =>
        {
            await _boardService.ListBoardsAsync(project, url);
        }, projectOption, urlOption);

        return command;
    }
}
