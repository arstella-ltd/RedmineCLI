using System.Text.Json.Serialization;

using RedmineCLI.Exceptions;

namespace RedmineCLI.Models;

public class Issue : IEquatable<Issue>
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("subject")]
    public string Subject { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("project")]
    public Project? Project { get; set; }

    [JsonPropertyName("status")]
    public IssueStatus? Status { get; set; }

    [JsonPropertyName("priority")]
    public Priority? Priority { get; set; }

    [JsonPropertyName("assigned_to")]
    public User? AssignedTo { get; set; }

    [JsonPropertyName("created_on")]
    public DateTime CreatedOn { get; set; }

    [JsonPropertyName("updated_on")]
    public DateTime UpdatedOn { get; set; }

    [JsonPropertyName("done_ratio")]
    public int? DoneRatio { get; set; }

    [JsonPropertyName("journals")]
    public List<Journal>? Journals { get; set; }

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Subject))
        {
            throw new ValidationException("Subject is required");
        }
    }

    public bool Equals(Issue? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return Id == other.Id;
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as Issue);
    }

    public override int GetHashCode()
    {
        return Id.GetHashCode();
    }
}
