namespace RedmineCLI.Commands;

/// <summary>
/// CommentAsyncメソッドのパラメータをまとめたオプションクラス
/// </summary>
public class IssueCommentOptions
{
    /// <summary>
    /// チケットID
    /// </summary>
    public int IssueId { get; set; }

    /// <summary>
    /// コメントのテキスト
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    /// チケットの新しいタイトル
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// チケットの新しいステータス
    /// </summary>
    public string? Status { get; set; }

    /// <summary>
    /// 進捗率（0-100）
    /// </summary>
    public int? DoneRatio { get; set; }

    /// <summary>
    /// 新しい説明文
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// 説明文を読み込むファイルパス（'-' で標準入力）
    /// </summary>
    public string? DescriptionFile { get; set; }

    /// <summary>
    /// 新しい担当者（ユーザー名、ID、または @me）
    /// </summary>
    public string? Assignee { get; set; }
}
