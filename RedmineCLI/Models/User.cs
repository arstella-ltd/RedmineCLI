using System.Text.Json.Serialization;

namespace RedmineCLI.Models;

public class User
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("login")]
    public string Login { get; set; } = string.Empty;

    [JsonPropertyName("firstname")]
    public string? FirstName { get; set; }

    [JsonPropertyName("lastname")]
    public string? LastName { get; set; }

    [JsonPropertyName("mail")]
    public string? Email { get; set; }

    [JsonIgnore]
    public string? Mail => Email;

    [JsonIgnore]
    public string Name
    {
        get
        {
            if (!string.IsNullOrEmpty(FirstName) || !string.IsNullOrEmpty(LastName))
            {
                return $"{FirstName} {LastName}".Trim();
            }
            // Fallback to login if name is not available
            return Login ?? string.Empty;
        }
    }

    [JsonPropertyName("created_on")]
    public DateTime CreatedOn { get; set; }
}
