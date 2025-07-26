using System.Text.Json.Serialization;

namespace RedmineCLI.Models;

public class User
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("login")]
    public string? Login { get; set; }

    [JsonPropertyName("firstname")]
    public string? FirstName { get; set; }

    [JsonPropertyName("lastname")]
    public string? LastName { get; set; }

    [JsonPropertyName("mail")]
    public string? Email { get; set; }

    [JsonIgnore]
    public string? Mail => Email;

    [JsonPropertyName("created_on")]
    public DateTime CreatedOn { get; set; }
}
