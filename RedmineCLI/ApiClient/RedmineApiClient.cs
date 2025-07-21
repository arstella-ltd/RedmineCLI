using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Microsoft.Extensions.Logging;
using RedmineCLI.Exceptions;
using RedmineCLI.Models;
using RedmineCLI.Services;

namespace RedmineCLI.ApiClient;

public class RedmineApiClient : IRedmineApiClient
{
    private readonly HttpClient _httpClient;
    private readonly IConfigService _configService;
    private readonly ILogger<RedmineApiClient> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public RedmineApiClient(
        HttpClient httpClient,
        IConfigService configService,
        ILogger<RedmineApiClient> logger)
    {
        _httpClient = httpClient;
        _configService = configService;
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions(RedmineJsonContext.Default.Options);
    }

    public async Task<IssuesResponse> GetIssuesAsync(
        int? assignedToId = null,
        int? projectId = null,
        string? status = null,
        int? limit = null,
        int? offset = null,
        CancellationToken cancellationToken = default)
    {
        var queryString = BuildQueryString(new Dictionary<string, string?>
        {
            ["assigned_to_id"] = assignedToId?.ToString(),
            ["project_id"] = projectId?.ToString(),
            ["status_id"] = status,
            ["limit"] = limit?.ToString(),
            ["offset"] = offset?.ToString()
        });

        var path = $"/issues.json{queryString}";
        return await GetAsync(path, RedmineJsonContext.Default.IssuesResponse, "issues", cancellationToken) 
               ?? new IssuesResponse();
    }

    public async Task<Issue> GetIssueAsync(int id, CancellationToken cancellationToken = default)
    {
        var path = $"/issues/{id}.json";
        var issueResponse = await GetAsync(path, RedmineJsonContext.Default.IssueResponse, $"get issue {id}", cancellationToken);
        
        return issueResponse?.Issue ?? throw new RedmineApiException(
            (int)HttpStatusCode.NotFound,
            $"Issue with ID {id} not found");
    }

    public async Task<Issue> CreateIssueAsync(Issue issue, CancellationToken cancellationToken = default)
    {
        var path = "/issues.json";
        var requestBody = new IssueRequest { Issue = issue };
        var issueResponse = await PostAsync(path, requestBody, RedmineJsonContext.Default.IssueRequest, RedmineJsonContext.Default.IssueResponse, "create issue", cancellationToken);

        return issueResponse?.Issue ?? throw new RedmineApiException(
            (int)HttpStatusCode.InternalServerError,
            "Failed to create issue");
    }

    public async Task<Issue> UpdateIssueAsync(int id, Issue issue, CancellationToken cancellationToken = default)
    {
        var path = $"/issues/{id}.json";
        var requestBody = new IssueRequest { Issue = issue };
        var issueResponse = await PutAsync(path, requestBody, RedmineJsonContext.Default.IssueRequest, RedmineJsonContext.Default.IssueResponse, $"update issue {id}", cancellationToken);

        return issueResponse?.Issue ?? throw new RedmineApiException(
            (int)HttpStatusCode.InternalServerError,
            "Failed to update issue");
    }

    public async Task AddCommentAsync(int issueId, string comment, CancellationToken cancellationToken = default)
    {
        var path = $"/issues/{issueId}.json";
        var requestBody = new CommentRequest
        {
            Issue = new CommentData { Notes = comment }
        };
        
        await PutAsyncVoid(path, requestBody, RedmineJsonContext.Default.CommentRequest, $"add comment to issue {issueId}", cancellationToken);
    }

    public async Task<List<Project>> GetProjectsAsync(CancellationToken cancellationToken = default)
    {
        var path = "/projects.json";
        var projectsResponse = await GetAsync(path, RedmineJsonContext.Default.ProjectsResponse, "projects", cancellationToken);
        return projectsResponse?.Projects ?? new List<Project>();
    }

    public async Task<List<User>> GetUsersAsync(CancellationToken cancellationToken = default)
    {
        var path = "/users.json";
        var usersResponse = await GetAsync(path, RedmineJsonContext.Default.UsersResponse, "users", cancellationToken);
        return usersResponse?.Users ?? new List<User>();
    }

    public async Task<List<IssueStatus>> GetIssueStatusesAsync(CancellationToken cancellationToken = default)
    {
        var path = "/issue_statuses.json";
        var statusesResponse = await GetAsync(path, RedmineJsonContext.Default.IssueStatusesResponse, "issue statuses", cancellationToken);
        return statusesResponse?.IssueStatuses ?? new List<IssueStatus>();
    }

    public async Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await EnsureAuthenticatedAsync();

            var path = "/users/current.json";
            _logger.LogDebug("Testing connection: {Path}", path);

            var response = await _httpClient.GetAsync(path, cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    private async Task EnsureAuthenticatedAsync()
    {
        var profile = await _configService.GetActiveProfileAsync();
        if (profile == null)
        {
            throw new InvalidOperationException("No active profile found. Please run 'redmine auth login' first.");
        }

        if (string.IsNullOrEmpty(profile.ApiKey))
        {
            throw new InvalidOperationException("No API key found in active profile. Please run 'redmine auth login' first.");
        }

        // Remove existing header if present
        _httpClient.DefaultRequestHeaders.Remove("X-Redmine-API-Key");
        _httpClient.DefaultRequestHeaders.Add("X-Redmine-API-Key", profile.ApiKey);

        if (_httpClient.BaseAddress == null && !string.IsNullOrEmpty(profile.Url))
        {
            _httpClient.BaseAddress = new Uri(profile.Url);
        }
    }

    private static async Task EnsureSuccessStatusCodeAsync(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode)
            return;

        var content = await response.Content.ReadAsStringAsync();
        var statusCode = (int)response.StatusCode;
        var reasonPhrase = response.ReasonPhrase ?? response.StatusCode.ToString();

        string errorMessage;
        try
        {
            var errorResponse = JsonSerializer.Deserialize(content, RedmineJsonContext.Default.ErrorResponse);
            errorMessage = errorResponse?.Errors?.FirstOrDefault() ?? reasonPhrase;
        }
        catch
        {
            errorMessage = string.IsNullOrEmpty(content) ? reasonPhrase : content;
        }

        throw new RedmineApiException(statusCode, $"{reasonPhrase}: {errorMessage}", content);
    }

    private static string BuildQueryString(Dictionary<string, string?> parameters)
    {
        var validParams = parameters
            .Where(kvp => !string.IsNullOrEmpty(kvp.Value))
            .Select(kvp => $"{kvp.Key}={Uri.EscapeDataString(kvp.Value!)}")
            .ToList();

        return validParams.Count > 0 ? "?" + string.Join("&", validParams) : string.Empty;
    }

    private async Task<T?> GetAsync<T>(string path, JsonTypeInfo<T> typeInfo, string operationName, CancellationToken cancellationToken)
    {
        await EnsureAuthenticatedAsync();
        _logger.LogDebug("Performing GET request for {Operation}: {Path}", operationName, path);

        try
        {
            var response = await _httpClient.GetAsync(path, cancellationToken);
            await EnsureSuccessStatusCodeAsync(response);

            var jsonContent = await response.Content.ReadAsStringAsync(cancellationToken);
            return JsonSerializer.Deserialize(jsonContent, typeInfo);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Network error while performing {Operation}", operationName);
            throw;
        }
    }

    private async Task<TResponse?> PostAsync<TRequest, TResponse>(string path, TRequest requestBody, JsonTypeInfo<TRequest> requestTypeInfo, JsonTypeInfo<TResponse> responseTypeInfo, string operationName, CancellationToken cancellationToken)
    {
        await EnsureAuthenticatedAsync();
        _logger.LogDebug("Performing POST request for {Operation}: {Path}", operationName, path);

        try
        {
            var json = JsonSerializer.Serialize(requestBody, requestTypeInfo);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(path, content, cancellationToken);
            await EnsureSuccessStatusCodeAsync(response);

            var jsonContent = await response.Content.ReadAsStringAsync(cancellationToken);
            return JsonSerializer.Deserialize(jsonContent, responseTypeInfo);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Network error while performing {Operation}", operationName);
            throw;
        }
    }

    private async Task<TResponse?> PutAsync<TRequest, TResponse>(string path, TRequest requestBody, JsonTypeInfo<TRequest> requestTypeInfo, JsonTypeInfo<TResponse> responseTypeInfo, string operationName, CancellationToken cancellationToken)
    {
        await EnsureAuthenticatedAsync();
        _logger.LogDebug("Performing PUT request for {Operation}: {Path}", operationName, path);

        try
        {
            var json = JsonSerializer.Serialize(requestBody, requestTypeInfo);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PutAsync(path, content, cancellationToken);
            await EnsureSuccessStatusCodeAsync(response);

            var jsonContent = await response.Content.ReadAsStringAsync(cancellationToken);
            return JsonSerializer.Deserialize(jsonContent, responseTypeInfo);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Network error while performing {Operation}", operationName);
            throw;
        }
    }

    private async Task PutAsyncVoid<TRequest>(string path, TRequest requestBody, JsonTypeInfo<TRequest> requestTypeInfo, string operationName, CancellationToken cancellationToken)
    {
        await EnsureAuthenticatedAsync();
        _logger.LogDebug("Performing PUT request for {Operation}: {Path}", operationName, path);

        try
        {
            var json = JsonSerializer.Serialize(requestBody, requestTypeInfo);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PutAsync(path, content, cancellationToken);
            await EnsureSuccessStatusCodeAsync(response);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Network error while performing {Operation}", operationName);
            throw;
        }
    }

}