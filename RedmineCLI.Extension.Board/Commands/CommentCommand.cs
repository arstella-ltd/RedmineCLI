using System.CommandLine;

using Microsoft.Extensions.Logging;

namespace RedmineCLI.Extension.Board.Commands;

/// <summary>
/// redmine-board comment コマンドの実装（replyコマンドのエイリアス）
/// </summary>
public class CommentCommand
{
    private readonly ReplyCommand _replyCommand;

    public CommentCommand(ReplyCommand replyCommand)
    {
        _replyCommand = replyCommand;
    }

    public Command Create()
    {
        // replyコマンドを基に作成し、名前と説明を変更
        var replyCommandBase = _replyCommand.Create();
        var command = new Command("comment", "Comment on a board topic (alias for reply)");

        // replyコマンドから引数とオプションをコピー
        foreach (var argument in replyCommandBase.Arguments)
        {
            command.Add(argument);
        }

        foreach (var option in replyCommandBase.Options)
        {
            command.Add(option);
        }

        // replyコマンドと同じハンドラーを使用
        command.Handler = replyCommandBase.Handler;

        return command;
    }
}
