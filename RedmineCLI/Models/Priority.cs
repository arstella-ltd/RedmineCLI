using System.Text.Json.Serialization;

namespace RedmineCLI.Models;

public class Priority
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
}
