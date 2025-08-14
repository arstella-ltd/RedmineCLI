using System.Globalization;

using Microsoft.Extensions.Logging;

using RedmineCLI.ApiClient;
using RedmineCLI.Exceptions;
using RedmineCLI.Models;

namespace RedmineCLI.Services;

/// <summary>
/// Redmineのビジネスロジックを提供するサービス
/// </summary>
public class RedmineService : IRedmineService
{
    private readonly IRedmineApiClient _apiClient;
    private readonly ILogger<RedmineService> _logger;

    // キャッシュ（メモリ内、短期間のみ保持）
    private User? _currentUserCache;
    private DateTime _currentUserCacheTime = DateTime.MinValue;
    private List<IssueStatus>? _statusesCache;
    private DateTime _statusesCacheTime = DateTime.MinValue;
    private List<Project>? _projectsCache;
    private DateTime _projectsCacheTime = DateTime.MinValue;
    private List<Priority>? _prioritiesCache;
    private DateTime _prioritiesCacheTime = DateTime.MinValue;
    private readonly TimeSpan _cacheExpiration = TimeSpan.FromMinutes(5);

    public RedmineService(IRedmineApiClient apiClient, ILogger<RedmineService> logger)
    {
        _apiClient = apiClient;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<List<Issue>> GetIssuesAsync(IssueFilter filter, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting issues with filter: {@Filter}", filter);

        // @meの解決
        if (filter.AssignedToId == "@me")
        {
            filter.AssignedToId = await ResolveAssigneeAsync("@me", cancellationToken);
        }

        return await _apiClient.GetIssuesAsync(filter, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<List<Issue>> SearchIssuesAsync(
        string searchQuery,
        string? assignedToId = null,
        string? statusId = null,
        string? projectId = null,
        int? limit = null,
        int? offset = null,
        string? sort = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Searching issues with query: {Query}", searchQuery);

        // @meの解決
        if (assignedToId == "@me")
        {
            assignedToId = await ResolveAssigneeAsync("@me", cancellationToken);
        }

        return await _apiClient.SearchIssuesAsync(
            searchQuery, assignedToId, statusId, projectId, limit, offset, sort, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<Issue> GetIssueAsync(int id, bool includeJournals = false, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting issue {IssueId}, includeJournals: {IncludeJournals}", id, includeJournals);
        return await _apiClient.GetIssueAsync(id, includeJournals, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<Issue> CreateIssueAsync(
        string projectIdOrIdentifier,
        string subject,
        string? description = null,
        string? assigneeIdOrUsername = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Creating issue in project {Project}", projectIdOrIdentifier);

        // プロジェクトIDの解決
        var projectId = await ResolveProjectIdAsync(projectIdOrIdentifier, cancellationToken);

        // 担当者の解決
        int? assignedToId = null;
        if (!string.IsNullOrEmpty(assigneeIdOrUsername))
        {
            var resolvedAssignee = await ResolveAssigneeAsync(assigneeIdOrUsername, cancellationToken);
            if (int.TryParse(resolvedAssignee, out var assigneeId))
            {
                assignedToId = assigneeId;
            }
        }

        var issue = new Issue
        {
            Subject = subject,
            Description = description,
            Project = new Project { Id = projectId },
            AssignedTo = assignedToId.HasValue ? new User { Id = assignedToId.Value } : null
        };

        return await _apiClient.CreateIssueAsync(issue, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<Issue> UpdateIssueAsync(
        int id,
        string? subject = null,
        string? statusIdOrName = null,
        string? assigneeIdOrUsername = null,
        string? description = null,
        int? doneRatio = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Updating issue {IssueId}", id);

        // 既存のチケットを取得（subjectが必須のため）
        var existingIssue = await _apiClient.GetIssueAsync(id, cancellationToken);

        var updateData = new Issue
        {
            Id = id,
            Subject = subject ?? existingIssue.Subject
        };

        // 説明の更新
        if (description != null)
        {
            updateData.Description = description;
        }

        // ステータスの解決
        if (!string.IsNullOrEmpty(statusIdOrName))
        {
            var statusId = await ResolveStatusIdAsync(statusIdOrName, cancellationToken);
            if (statusId.HasValue)
            {
                updateData.Status = new IssueStatus { Id = statusId.Value };
            }
        }

        // 担当者の解決
        if (!string.IsNullOrEmpty(assigneeIdOrUsername))
        {
            if (assigneeIdOrUsername == "__REMOVE__")
            {
                // 担当者を削除するために特別な値を設定
                updateData.AssignedTo = new User { Id = -1 };
            }
            else
            {
                var resolvedAssignee = await ResolveAssigneeAsync(assigneeIdOrUsername, cancellationToken);
                if (int.TryParse(resolvedAssignee, out var assigneeId))
                {
                    updateData.AssignedTo = new User { Id = assigneeId };
                }
            }
        }

        // 進捗率
        if (doneRatio.HasValue)
        {
            if (doneRatio.Value < 0 || doneRatio.Value > 100)
            {
                throw new ValidationException("Progress must be between 0 and 100");
            }
            updateData.DoneRatio = doneRatio.Value;
        }

        return await _apiClient.UpdateIssueAsync(id, updateData, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task AddCommentAsync(int issueId, string comment, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Adding comment to issue {IssueId}", issueId);
        await _apiClient.AddCommentAsync(issueId, comment, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<string> ResolveAssigneeAsync(string? assigneeIdOrUsername, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(assigneeIdOrUsername))
        {
            return string.Empty;
        }

        // 数値の場合はそのまま返す
        if (int.TryParse(assigneeIdOrUsername, out _))
        {
            return assigneeIdOrUsername;
        }

        // @meの場合は現在のユーザーIDを返す
        if (assigneeIdOrUsername == "@me")
        {
            var currentUser = await GetCurrentUserAsync(cancellationToken);
            return currentUser.Id.ToString(CultureInfo.InvariantCulture);
        }

        // それ以外はユーザー名として扱う（現状はそのまま返す）
        // 将来的にはユーザー名からIDへの変換を実装
        return assigneeIdOrUsername;
    }

    /// <inheritdoc/>
    public async Task<int?> ResolveStatusIdAsync(string? statusIdOrName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(statusIdOrName))
        {
            return null;
        }

        // 数値の場合はそのまま返す
        if (int.TryParse(statusIdOrName, out var statusId))
        {
            return statusId;
        }

        // ステータス一覧から名前で検索
        var statuses = await GetCachedStatusesAsync(cancellationToken);
        var status = statuses.FirstOrDefault(s =>
            s.Name.Equals(statusIdOrName, StringComparison.OrdinalIgnoreCase));

        if (status == null)
        {
            throw new ValidationException($"Status '{statusIdOrName}' not found");
        }

        return status.Id;
    }

    /// <inheritdoc/>
    public async Task<int?> ResolvePriorityIdAsync(string? priorityIdOrName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(priorityIdOrName))
        {
            return null;
        }

        // 数値の場合はそのまま返す
        if (int.TryParse(priorityIdOrName, out var priorityId))
        {
            return priorityId;
        }

        // 優先度一覧から名前で検索
        var priorities = await GetCachedPrioritiesAsync(cancellationToken);
        var priority = priorities.FirstOrDefault(p =>
            p.Name.Equals(priorityIdOrName, StringComparison.OrdinalIgnoreCase));

        if (priority == null)
        {
            throw new ValidationException($"Priority '{priorityIdOrName}' not found");
        }

        return priority.Id;
    }

    /// <inheritdoc/>
    public async Task<int> ResolveProjectIdAsync(string projectIdOrIdentifier, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(projectIdOrIdentifier))
        {
            throw new ValidationException("Project ID or identifier is required");
        }

        // 数値の場合はそのまま返す
        if (int.TryParse(projectIdOrIdentifier, out var projectId))
        {
            return projectId;
        }

        // プロジェクト一覧から識別子で検索
        var projects = await GetCachedProjectsAsync(cancellationToken);
        var project = projects.FirstOrDefault(p =>
            p.Identifier?.Equals(projectIdOrIdentifier, StringComparison.OrdinalIgnoreCase) == true ||
            p.Name.Equals(projectIdOrIdentifier, StringComparison.OrdinalIgnoreCase));

        if (project == null)
        {
            throw new ValidationException($"Project '{projectIdOrIdentifier}' not found");
        }

        return project.Id;
    }

    /// <inheritdoc/>
    public async Task<List<Project>> GetProjectsAsync(CancellationToken cancellationToken = default)
    {
        return await GetCachedProjectsAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<List<User>> GetUsersAsync(int? limit = null, CancellationToken cancellationToken = default)
    {
        return await _apiClient.GetUsersAsync(limit, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<List<IssueStatus>> GetIssueStatusesAsync(CancellationToken cancellationToken = default)
    {
        return await GetCachedStatusesAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<List<Priority>> GetPrioritiesAsync(CancellationToken cancellationToken = default)
    {
        return await GetCachedPrioritiesAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<User> GetCurrentUserAsync(CancellationToken cancellationToken = default)
    {
        // キャッシュが有効な場合は返す
        if (_currentUserCache != null && DateTime.UtcNow - _currentUserCacheTime < _cacheExpiration)
        {
            return _currentUserCache;
        }

        _logger.LogDebug("Fetching current user from API");
        var user = await _apiClient.GetCurrentUserAsync(cancellationToken);

        // キャッシュに保存
        _currentUserCache = user;
        _currentUserCacheTime = DateTime.UtcNow;

        return user;
    }

    /// <inheritdoc/>
    public async Task<Attachment> GetAttachmentAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _apiClient.GetAttachmentAsync(id, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<Stream> DownloadAttachmentAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _apiClient.DownloadAttachmentAsync(id, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<Stream> DownloadAttachmentAsync(string contentUrl, CancellationToken cancellationToken = default)
    {
        return await _apiClient.DownloadAttachmentAsync(contentUrl, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default)
    {
        return await _apiClient.TestConnectionAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<bool> TestConnectionAsync(string url, string apiKey, CancellationToken cancellationToken = default)
    {
        return await _apiClient.TestConnectionAsync(url, apiKey, cancellationToken);
    }

    /// <summary>
    /// キャッシュされたステータス一覧を取得する
    /// </summary>
    private async Task<List<IssueStatus>> GetCachedStatusesAsync(CancellationToken cancellationToken)
    {
        // キャッシュが有効な場合は返す
        if (_statusesCache != null && DateTime.UtcNow - _statusesCacheTime < _cacheExpiration)
        {
            return _statusesCache;
        }

        _logger.LogDebug("Fetching issue statuses from API");
        var statuses = await _apiClient.GetIssueStatusesAsync(cancellationToken);

        // キャッシュに保存
        _statusesCache = statuses;
        _statusesCacheTime = DateTime.UtcNow;

        return statuses;
    }

    /// <summary>
    /// キャッシュされたプロジェクト一覧を取得する
    /// </summary>
    private async Task<List<Project>> GetCachedProjectsAsync(CancellationToken cancellationToken)
    {
        // キャッシュが有効な場合は返す
        if (_projectsCache != null && DateTime.UtcNow - _projectsCacheTime < _cacheExpiration)
        {
            return _projectsCache;
        }

        _logger.LogDebug("Fetching projects from API");
        var projects = await _apiClient.GetProjectsAsync(cancellationToken);

        // キャッシュに保存
        _projectsCache = projects;
        _projectsCacheTime = DateTime.UtcNow;

        return projects;
    }

    /// <summary>
    /// キャッシュされた優先度一覧を取得する
    /// </summary>
    private async Task<List<Priority>> GetCachedPrioritiesAsync(CancellationToken cancellationToken)
    {
        // キャッシュが有効な場合は返す
        if (_prioritiesCache != null && DateTime.UtcNow - _prioritiesCacheTime < _cacheExpiration)
        {
            return _prioritiesCache;
        }

        _logger.LogDebug("Fetching issue priorities from API");
        var priorities = await _apiClient.GetPrioritiesAsync(cancellationToken);

        // キャッシュに保存
        _prioritiesCache = priorities;
        _prioritiesCacheTime = DateTime.UtcNow;

        return priorities;
    }
}
