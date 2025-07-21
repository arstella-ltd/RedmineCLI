using System.Text.Json.Serialization;
using RedmineCLI.Models;

namespace RedmineCLI.ApiClient;

[JsonSerializable(typeof(Issue))]
[JsonSerializable(typeof(Issue[]))]
[JsonSerializable(typeof(List<Issue>))]
[JsonSerializable(typeof(IssueResponse))]
[JsonSerializable(typeof(IssuesResponse))]
[JsonSerializable(typeof(Project))]
[JsonSerializable(typeof(Project[]))]
[JsonSerializable(typeof(List<Project>))]
[JsonSerializable(typeof(ProjectsResponse))]
[JsonSerializable(typeof(User))]
[JsonSerializable(typeof(User[]))]
[JsonSerializable(typeof(List<User>))]
[JsonSerializable(typeof(UsersResponse))]
[JsonSerializable(typeof(IssueStatus))]
[JsonSerializable(typeof(IssueStatus[]))]
[JsonSerializable(typeof(List<IssueStatus>))]
[JsonSerializable(typeof(IssueStatusesResponse))]
[JsonSerializable(typeof(Priority))]
[JsonSerializable(typeof(ErrorResponse))]
[JsonSerializable(typeof(IssueRequest))]
[JsonSerializable(typeof(CommentRequest))]
[JsonSerializable(typeof(CommentData))]
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower,
    WriteIndented = true,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    GenerationMode = JsonSourceGenerationMode.Default)]
public partial class RedmineJsonContext : JsonSerializerContext
{
}