using System.Text.Json.Serialization;

namespace RedmineCLI.Models;

public class User
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("login")]
    public string Login { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("firstname")]
    public string? FirstName { get; set; }

    [JsonPropertyName("lastname")]
    public string? LastName { get; set; }

    [JsonPropertyName("mail")]
    public string? Email { get; set; }

    [JsonIgnore]
    public string? Mail => Email;

    [JsonIgnore]
    public string DisplayName
    {
        get
        {
            // If name is provided (e.g., from issues API), use it
            if (!string.IsNullOrEmpty(Name))
            {
                return Name;
            }
            // Otherwise, build from firstname and lastname (e.g., from users API)
            if (!string.IsNullOrEmpty(FirstName) || !string.IsNullOrEmpty(LastName))
            {
                return $"{FirstName} {LastName}".Trim();
            }
            // Fallback to login
            return Login ?? string.Empty;
        }
    }

    [JsonPropertyName("created_on")]
    public DateTime CreatedOn { get; set; }
}
