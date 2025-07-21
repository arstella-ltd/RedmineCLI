using RedmineCLI.Models;

namespace RedmineCLI.Services;

public interface IConfigService
{
    Task<Config> LoadConfigAsync();
    Task SaveConfigAsync(Config config);
    Task<Profile?> GetActiveProfileAsync();
    Task SwitchProfileAsync(string profileName);
    Task CreateProfileAsync(Profile profile);
    Task DeleteProfileAsync(string profileName);
    Task UpdatePreferencesAsync(string key, string value);
}