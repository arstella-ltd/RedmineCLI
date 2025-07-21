using System.Text.Json.Serialization;

namespace RedmineCLI.Models;

public class Project
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("identifier")]
    public string? Identifier { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("created_on")]
    public DateTime? CreatedOn { get; set; }

    [JsonPropertyName("updated_on")]
    public DateTime? UpdatedOn { get; set; }
}
