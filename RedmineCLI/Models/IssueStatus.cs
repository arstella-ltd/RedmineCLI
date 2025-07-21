using System.Text.Json.Serialization;

namespace RedmineCLI.Models;

public class IssueStatus
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("is_closed")]
    public bool? IsClosed { get; set; }
}
