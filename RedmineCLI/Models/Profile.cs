using VYaml.Annotations;

namespace RedmineCLI.Models;

[YamlObject]
public partial class Profile
{
    public string Name { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string? DefaultProject { get; set; }
    public Preferences? Preferences { get; set; }
}
