using System.CommandLine;

using Microsoft.Extensions.Logging;

using RedmineCLI.Extension.Board.Services;

namespace RedmineCLI.Extension.Board.Commands;

/// <summary>
/// ボードのトピック操作を管理するコマンド
/// </summary>
public class BoardTopicCommand
{
    private readonly ILogger<BoardTopicCommand> _logger;
    private readonly IBoardService _boardService;
    private readonly IAuthenticationService _authenticationService;

    public BoardTopicCommand(
        ILogger<BoardTopicCommand> logger,
        IBoardService boardService,
        IAuthenticationService authenticationService)
    {
        _logger = logger;
        _boardService = boardService;
        _authenticationService = authenticationService;
    }

    /// <summary>
    /// 動的にボードIDベースのコマンドを作成する
    /// </summary>
    public Command? CreateDynamicBoardCommand(string[] args)
    {
        // Check if first arg is a number (board ID)
        if (args.Length > 0 && int.TryParse(args[0], out _))
        {
            var boardId = args[0];

            // Create a command for this specific board ID
            var boardCommand = new Command(boardId, $"Operations for board {boardId}");
            boardCommand.IsHidden = true; // Hide from help

            // Create topic command structure under this board command
            var topicCommand = new Command("topic", "Topic operations");

            // Topic list subcommand
            var topicListCommand = new Command("list", "List topics in the board");
            topicListCommand.AddAlias("ls");
            var topicListProjectOption = new Option<string>("--project", "Project name or ID");
            topicListCommand.Add(topicListProjectOption);
            topicListCommand.SetHandler(async (string? project) =>
            {
                var (url, sessionCookie) = await _authenticationService.GetAuthenticationAsync(null);
                if (!string.IsNullOrEmpty(sessionCookie))
                {
                    await _boardService.ListTopicsAsync(boardId, project, (sessionCookie, url));
                }
            }, topicListProjectOption);

            // Topic view command (when topic ID is provided)
            var topicIdArgument = new Argument<string>("topic-id", "Topic ID");
            topicCommand.Add(topicIdArgument);
            var topicViewProjectOption = new Option<string>("--project", "Project name or ID");
            topicCommand.Add(topicViewProjectOption);
            topicCommand.SetHandler(async (string topicId, string? project) =>
            {
                var (url, sessionCookie) = await _authenticationService.GetAuthenticationAsync(null);
                if (!string.IsNullOrEmpty(sessionCookie))
                {
                    await _boardService.ViewTopicAsync(boardId, topicId, project, (sessionCookie, url));
                }
            }, topicIdArgument, topicViewProjectOption);

            topicCommand.AddCommand(topicListCommand);
            boardCommand.AddCommand(topicCommand);

            return boardCommand;
        }

        return null;
    }
}
