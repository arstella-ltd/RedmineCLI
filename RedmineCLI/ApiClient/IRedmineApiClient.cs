using RedmineCLI.Models;

namespace RedmineCLI.ApiClient;

public interface IRedmineApiClient
{
    Task<IssuesResponse> GetIssuesAsync(
        int? assignedToId = null,
        int? projectId = null,
        string? status = null,
        int? limit = null,
        int? offset = null,
        CancellationToken cancellationToken = default);

    Task<List<Issue>> GetIssuesAsync(IssueFilter filter, CancellationToken cancellationToken = default);

    Task<User> GetCurrentUserAsync(CancellationToken cancellationToken = default);

    Task<Issue> GetIssueAsync(int id, CancellationToken cancellationToken = default);

    Task<Issue> GetIssueAsync(int id, bool includeJournals, CancellationToken cancellationToken = default);

    Task<Issue> CreateIssueAsync(Issue issue, CancellationToken cancellationToken = default);

    Task<Issue> UpdateIssueAsync(int id, Issue issue, CancellationToken cancellationToken = default);

    Task AddCommentAsync(int issueId, string comment, CancellationToken cancellationToken = default);

    Task<List<Project>> GetProjectsAsync(CancellationToken cancellationToken = default);

    Task<List<User>> GetUsersAsync(CancellationToken cancellationToken = default);

    Task<List<IssueStatus>> GetIssueStatusesAsync(CancellationToken cancellationToken = default);

    Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default);

    Task<bool> TestConnectionAsync(string url, string apiKey, CancellationToken cancellationToken = default);

    Task<Attachment> GetAttachmentAsync(int id, CancellationToken cancellationToken = default);

    Task<Stream> DownloadAttachmentAsync(int id, CancellationToken cancellationToken = default);

    Task<Stream> DownloadAttachmentAsync(string contentUrl, CancellationToken cancellationToken = default);

    Task<List<Issue>> SearchIssuesAsync(
        string searchQuery,
        string? assignedToId = null,
        string? statusId = null,
        string? projectId = null,
        int? limit = null,
        int? offset = null,
        CancellationToken cancellationToken = default);
}
