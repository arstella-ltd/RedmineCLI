using VYaml.Annotations;

namespace RedmineCLI.Models;

[YamlObject]
public partial class TimeSettings
{
    public string Format { get; set; } = "relative"; // relative | absolute | utc
    public string Timezone { get; set; } = "system"; // system | UTC | Asia/Tokyo など
}
