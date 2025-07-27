namespace RedmineCLI.Commands;

/// <summary>
/// ListAsyncメソッドのパラメータをまとめたオプションクラス
/// </summary>
public class IssueListOptions
{
    /// <summary>
    /// 担当者フィルター（ユーザー名、ID、または @me）
    /// </summary>
    public string? Assignee { get; set; }

    /// <summary>
    /// ステータスフィルター（open、closed、all、またはステータスID）
    /// </summary>
    public string? Status { get; set; }

    /// <summary>
    /// プロジェクトフィルター（識別子またはID）
    /// </summary>
    public string? Project { get; set; }

    /// <summary>
    /// 結果の表示件数制限（デフォルト: 30）
    /// </summary>
    public int? Limit { get; set; }

    /// <summary>
    /// ページネーション用のオフセット
    /// </summary>
    public int? Offset { get; set; }

    /// <summary>
    /// JSON形式で出力するかどうか
    /// </summary>
    public bool Json { get; set; }

    /// <summary>
    /// Webブラウザで開くかどうか
    /// </summary>
    public bool Web { get; set; }

    /// <summary>
    /// 相対時刻ではなく絶対時刻を表示するかどうか
    /// </summary>
    public bool AbsoluteTime { get; set; }

    /// <summary>
    /// チケットのタイトルと説明で検索するキーワード
    /// </summary>
    public string? Search { get; set; }

    /// <summary>
    /// ソート条件（例: updated_on:desc, priority:desc,id）
    /// </summary>
    public string? Sort { get; set; }

    /// <summary>
    /// 優先度フィルター（名前またはID）
    /// </summary>
    public string? Priority { get; set; }

    /// <summary>
    /// 作成者フィルター（ユーザー名、ID、または @me）
    /// </summary>
    public string? Author { get; set; }
}
