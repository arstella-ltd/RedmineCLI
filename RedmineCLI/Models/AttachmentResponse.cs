using System.Text.Json.Serialization;

namespace RedmineCLI.Models;

public class AttachmentResponse
{
    [JsonPropertyName("attachment")]
    public Attachment? Attachment { get; set; }
}