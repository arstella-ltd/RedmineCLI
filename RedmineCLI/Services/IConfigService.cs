using RedmineCLI.Models;

namespace RedmineCLI.Services;

public interface IConfigService
{
    Config GetConfig();
    void SaveConfig(Config config);
    Profile GetCurrentProfile();
    void SetCurrentProfile(string profileName);
    void CreateProfile(string name, string url, string apiKey);
    void DeleteProfile(string name);
    void UpdateProfile(string name, string? url = null, string? apiKey = null);
    string GetConfigPath();
}