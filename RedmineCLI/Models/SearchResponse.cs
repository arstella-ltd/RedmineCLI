using System.Text.Json.Serialization;

namespace RedmineCLI.Models;

public class SearchResponse
{
    [JsonPropertyName("results")]
    public List<SearchResult>? Results { get; set; }

    [JsonPropertyName("total_count")]
    public int TotalCount { get; set; }

    [JsonPropertyName("offset")]
    public int Offset { get; set; }

    [JsonPropertyName("limit")]
    public int Limit { get; set; }
}