using VYaml.Annotations;

namespace RedmineCLI.Models;

[YamlObject]
public partial class Profile
{
    public string Name { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string? DefaultProject { get; set; }
    public string? UserName { get; set; }

    // TimeFormatを文字列としてシリアライズ（互換性とAOT互換性のため）
    [YamlMember("TimeFormat")]
    public string TimeFormatString
    {
        get => TimeFormat.ToString().ToLower();
        set
        {
            if (string.IsNullOrEmpty(value))
            {
                TimeFormat = TimeFormat.Relative;
                return;
            }

            TimeFormat = value.ToLower() switch
            {
                "relative" => TimeFormat.Relative,
                "absolute" => TimeFormat.Absolute,
                "utc" => TimeFormat.Utc,
                _ => TimeFormat.Relative
            };
        }
    }

    [YamlIgnore]
    public TimeFormat TimeFormat { get; set; } = TimeFormat.Relative;

    public string OutputFormat { get; set; } = "table";
    public Preferences? Preferences { get; set; }
}
