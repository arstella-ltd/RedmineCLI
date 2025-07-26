using RedmineCLI.Models;

namespace RedmineCLI.Services;

/// <summary>
/// Redmineのビジネスロジックを提供するサービス
/// </summary>
public interface IRedmineService
{
    /// <summary>
    /// チケット一覧を取得する
    /// </summary>
    Task<List<Issue>> GetIssuesAsync(IssueFilter filter, CancellationToken cancellationToken = default);

    /// <summary>
    /// チケットを検索する
    /// </summary>
    Task<List<Issue>> SearchIssuesAsync(
        string searchQuery,
        string? assignedToId = null,
        string? statusId = null,
        string? projectId = null,
        int? limit = null,
        int? offset = null,
        string? sort = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// チケットを取得する
    /// </summary>
    Task<Issue> GetIssueAsync(int id, bool includeJournals = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// チケットを作成する
    /// </summary>
    Task<Issue> CreateIssueAsync(
        string projectIdOrIdentifier,
        string subject,
        string? description = null,
        string? assigneeIdOrUsername = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// チケットを更新する
    /// </summary>
    Task<Issue> UpdateIssueAsync(
        int id,
        string? subject = null,
        string? statusIdOrName = null,
        string? assigneeIdOrUsername = null,
        int? doneRatio = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// チケットにコメントを追加する
    /// </summary>
    Task AddCommentAsync(int issueId, string comment, CancellationToken cancellationToken = default);

    /// <summary>
    /// @meを現在のユーザーIDに解決する
    /// </summary>
    Task<string> ResolveAssigneeAsync(string? assigneeIdOrUsername, CancellationToken cancellationToken = default);

    /// <summary>
    /// ステータス名をIDに解決する
    /// </summary>
    Task<int?> ResolveStatusIdAsync(string? statusIdOrName, CancellationToken cancellationToken = default);

    /// <summary>
    /// プロジェクト識別子/名前をIDに解決する
    /// </summary>
    Task<int> ResolveProjectIdAsync(string projectIdOrIdentifier, CancellationToken cancellationToken = default);

    /// <summary>
    /// プロジェクト一覧を取得する
    /// </summary>
    Task<List<Project>> GetProjectsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// ユーザー一覧を取得する
    /// </summary>
    Task<List<User>> GetUsersAsync(int? limit = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// ステータス一覧を取得する
    /// </summary>
    Task<List<IssueStatus>> GetIssueStatusesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 現在のユーザーを取得する
    /// </summary>
    Task<User> GetCurrentUserAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 添付ファイルを取得する
    /// </summary>
    Task<Attachment> GetAttachmentAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// 添付ファイルをダウンロードする
    /// </summary>
    Task<Stream> DownloadAttachmentAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// 添付ファイルをURLからダウンロードする
    /// </summary>
    Task<Stream> DownloadAttachmentAsync(string contentUrl, CancellationToken cancellationToken = default);

    /// <summary>
    /// 接続テストを行う
    /// </summary>
    Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 指定されたURLとAPIキーで接続テストを行う
    /// </summary>
    Task<bool> TestConnectionAsync(string url, string apiKey, CancellationToken cancellationToken = default);
}