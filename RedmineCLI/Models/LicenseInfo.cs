using System.Collections.Generic;

namespace RedmineCLI.Models;

public class LicenseInfo
{
    public string Name { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string License { get; set; } = string.Empty;
    public Dictionary<string, string> Dependencies { get; set; } = new();
}

public class LicenseData
{
    public string Name { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string License { get; set; } = string.Empty;
    public string ProjectUrl { get; set; } = string.Empty;
}
