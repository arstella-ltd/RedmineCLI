using RedmineCLI.Extension.Board.Models;

namespace RedmineCLI.Extension.Board.Services;

/// <summary>
/// HTML解析のためのサービスインターフェース
/// </summary>
public interface IHtmlParsingService
{
    /// <summary>
    /// HTMLからボード一覧を解析する
    /// </summary>
    /// <param name="html">HTML文字列</param>
    /// <param name="baseUrl">ベースURL</param>
    /// <returns>ボードのリスト</returns>
    List<Models.Board> ParseBoardsFromHtml(string html, string baseUrl);

    /// <summary>
    /// HTMLからトピック一覧を解析する
    /// </summary>
    /// <param name="html">HTML文字列</param>
    /// <returns>トピックのリスト</returns>
    List<Topic> ParseTopicsFromHtml(string html);

    /// <summary>
    /// HTMLからトピック詳細を解析する
    /// </summary>
    /// <param name="html">HTML文字列</param>
    /// <returns>トピック詳細</returns>
    TopicDetail? ParseTopicDetailFromHtml(string html);
}
