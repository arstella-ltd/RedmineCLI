using System.Text.Json.Serialization;
using RedmineCLI.Models;

namespace RedmineCLI.ApiClient;

public class IssueResponse
{
    [JsonPropertyName("issue")]
    public Issue? Issue { get; set; }
}

public class IssuesResponse
{
    [JsonPropertyName("issues")]
    public List<Issue> Issues { get; set; } = new();

    [JsonPropertyName("total_count")]
    public int TotalCount { get; set; }

    [JsonPropertyName("offset")]
    public int Offset { get; set; }

    [JsonPropertyName("limit")]
    public int Limit { get; set; }
}

public class ProjectsResponse
{
    [JsonPropertyName("projects")]
    public List<Project> Projects { get; set; } = new();

    [JsonPropertyName("total_count")]
    public int TotalCount { get; set; }

    [JsonPropertyName("offset")]
    public int Offset { get; set; }

    [JsonPropertyName("limit")]
    public int Limit { get; set; }
}

public class UsersResponse
{
    [JsonPropertyName("users")]
    public List<User> Users { get; set; } = new();

    [JsonPropertyName("total_count")]
    public int TotalCount { get; set; }

    [JsonPropertyName("offset")]
    public int Offset { get; set; }

    [JsonPropertyName("limit")]
    public int Limit { get; set; }
}

public class IssueStatusesResponse
{
    [JsonPropertyName("issue_statuses")]
    public List<IssueStatus> IssueStatuses { get; set; } = new();
}

public class ErrorResponse
{
    [JsonPropertyName("errors")]
    public string[]? Errors { get; set; }
}

public class IssueRequest
{
    [JsonPropertyName("issue")]
    public Issue Issue { get; set; } = new();
}

public class IssueCreateRequest
{
    [JsonPropertyName("issue")]
    public IssueCreateData Issue { get; set; } = new();
}

public class IssueCreateData
{
    [JsonPropertyName("project_id")]
    public int? ProjectId { get; set; }

    [JsonPropertyName("subject")]
    public string Subject { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("assigned_to_id")]
    public int? AssignedToId { get; set; }

    [JsonPropertyName("priority_id")]
    public int? PriorityId { get; set; }

    [JsonPropertyName("status_id")]
    public int? StatusId { get; set; }
}

public class CommentRequest
{
    [JsonPropertyName("issue")]
    public CommentData Issue { get; set; } = new();
}

public class CommentData
{
    [JsonPropertyName("notes")]
    public string Notes { get; set; } = string.Empty;
}

public class UserResponse
{
    [JsonPropertyName("user")]
    public User? User { get; set; }
}