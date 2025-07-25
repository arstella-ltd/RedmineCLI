using System.Text.Json.Serialization;

namespace RedmineCLI.Models;

public class SearchResult
{
    [JsonPropertyName("id")]
    public int? Id { get; set; }

    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("url")]
    public string? Url { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("datetime")]
    public DateTime? Datetime { get; set; }

    [JsonPropertyName("project")]
    public Project? Project { get; set; }
}