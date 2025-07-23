using System.Text.Json.Serialization;

namespace RedmineCLI.Models;

public class Attachment
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("filename")]
    public string Filename { get; set; } = string.Empty;

    [JsonPropertyName("filesize")]
    public long Filesize { get; set; }

    [JsonPropertyName("content_type")]
    public string ContentType { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("content_url")]
    public string ContentUrl { get; set; } = string.Empty;

    [JsonPropertyName("author")]
    public User? Author { get; set; }

    [JsonPropertyName("created_on")]
    public DateTime CreatedOn { get; set; }
}
