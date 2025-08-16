using RedmineCLI.Extension.Board.Models;

namespace RedmineCLI.Extension.Board.Services;

/// <summary>
/// ボード関連操作のためのサービスインターフェース
/// </summary>
public interface IBoardService
{
    /// <summary>
    /// ボード一覧を取得する
    /// </summary>
    /// <param name="projectFilter">プロジェクトフィルター</param>
    /// <param name="urlOverride">URLオーバーライド（オプション）</param>
    Task ListBoardsAsync(string? projectFilter, string? urlOverride);

    /// <summary>
    /// トピック一覧を取得する
    /// </summary>
    /// <param name="boardIdString">ボードID</param>
    /// <param name="projectName">プロジェクト名</param>
    /// <param name="auth">認証情報（セッションクッキーとベースURL）</param>
    Task ListTopicsAsync(string boardIdString, string? projectName, (string SessionCookie, string BaseUrl) auth);

    /// <summary>
    /// トピック詳細を表示する
    /// </summary>
    /// <param name="boardIdString">ボードID</param>
    /// <param name="topicIdString">トピックID</param>
    /// <param name="projectName">プロジェクト名</param>
    /// <param name="auth">認証情報（セッションクッキーとベースURL）</param>
    Task ViewTopicAsync(string boardIdString, string topicIdString, string? projectName, (string SessionCookie, string BaseUrl) auth);
}
