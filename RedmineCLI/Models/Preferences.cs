using VYaml.Annotations;

namespace RedmineCLI.Models;

[YamlObject]
public partial class Preferences
{
    public string DefaultFormat { get; set; } = "table";
    public int PageSize { get; set; } = 20;
    public bool UseColors { get; set; } = true;
    public string DateFormat { get; set; } = "yyyy-MM-dd HH:mm:ss";
    public string? Editor { get; set; }
    public string TimeFormat { get; set; } = "HH:mm:ss";
}
