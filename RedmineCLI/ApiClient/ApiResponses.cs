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