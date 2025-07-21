using System.IO.Abstractions;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using RedmineCLI.Exceptions;
using RedmineCLI.Models;
using VYaml.Serialization;

namespace RedmineCLI.Services;

public class ConfigService : IConfigService
{
    private readonly IFileSystem _fileSystem;
    private readonly string _configPath;

    public ConfigService() : this(new FileSystem())
    {
    }

    public ConfigService(IFileSystem fileSystem)
    {
        _fileSystem = fileSystem;
        _configPath = GetConfigPath();
    }

    public async Task<Config> LoadConfigAsync()
    {
        if (!_fileSystem.File.Exists(_configPath))
        {
            return new Config
            {
                CurrentProfile = "default",
                Profiles = new Dictionary<string, Profile>
                {
                    ["default"] = new Profile { Name = "default" }
                },
                Preferences = new Preferences()
            };
        }

        try
        {
            var yaml = await _fileSystem.File.ReadAllTextAsync(_configPath);
            var yamlBytes = System.Text.Encoding.UTF8.GetBytes(yaml);
            var config = YamlSerializer.Deserialize<Config>(yamlBytes);
            
            // Decrypt API keys after loading
            foreach (var profile in config.Profiles.Values)
            {
                if (!string.IsNullOrEmpty(profile.ApiKey))
                {
                    profile.ApiKey = DecryptApiKey(profile.ApiKey);
                }
            }
            
            return config;
        }
        catch (Exception ex)
        {
            throw new ValidationException($"設定ファイルの読み込みに失敗しました: {ex.Message}", ex);
        }
    }

    public async Task SaveConfigAsync(Config config)
    {
        try
        {
            var directory = _fileSystem.Path.GetDirectoryName(_configPath);
            if (!string.IsNullOrEmpty(directory) && !_fileSystem.Directory.Exists(directory))
            {
                _fileSystem.Directory.CreateDirectory(directory);
            }

            // Create a copy to avoid modifying the original
            var configToSave = new Config
            {
                CurrentProfile = config.CurrentProfile,
                Profiles = new Dictionary<string, Profile>(),
                Preferences = config.Preferences
            };

            // Encrypt API keys before saving
            foreach (var kvp in config.Profiles)
            {
                var profile = kvp.Value;
                var profileToSave = new Profile
                {
                    Name = profile.Name,
                    Url = profile.Url,
                    ApiKey = string.IsNullOrEmpty(profile.ApiKey) ? string.Empty : EncryptApiKey(profile.ApiKey),
                    DefaultProject = profile.DefaultProject,
                    Preferences = profile.Preferences
                };
                configToSave.Profiles[kvp.Key] = profileToSave;
            }

            var yaml = YamlSerializer.SerializeToString(configToSave);
            await _fileSystem.File.WriteAllTextAsync(_configPath, yaml);
        }
        catch (Exception ex)
        {
            throw new ValidationException($"設定ファイルの保存に失敗しました: {ex.Message}", ex);
        }
    }

    public async Task<Profile?> GetActiveProfileAsync()
    {
        var config = await LoadConfigAsync();
        if (string.IsNullOrEmpty(config.CurrentProfile))
        {
            return null;
        }

        return config.Profiles.TryGetValue(config.CurrentProfile, out var profile) ? profile : null;
    }

    public async Task SwitchProfileAsync(string profileName)
    {
        var config = await LoadConfigAsync();
        
        if (!config.Profiles.ContainsKey(profileName))
        {
            throw new ValidationException($"プロファイル '{profileName}' が見つかりません");
        }

        config.CurrentProfile = profileName ?? string.Empty;
        await SaveConfigAsync(config);
    }

    public async Task CreateProfileAsync(Profile profile)
    {
        if (string.IsNullOrEmpty(profile.Name))
        {
            throw new ValidationException("プロファイル名は必須です");
        }

        var config = await LoadConfigAsync();
        
        if (config.Profiles.ContainsKey(profile.Name))
        {
            throw new ValidationException($"プロファイル '{profile.Name}' は既に存在します");
        }

        config.Profiles[profile.Name] = profile;
        
        // Set as active if it's the first profile
        if (string.IsNullOrEmpty(config.CurrentProfile))
        {
            config.CurrentProfile = profile.Name;
        }

        await SaveConfigAsync(config);
    }

    public async Task DeleteProfileAsync(string profileName)
    {
        var config = await LoadConfigAsync();
        
        if (!config.Profiles.ContainsKey(profileName))
        {
            throw new ValidationException($"プロファイル '{profileName}' が見つかりません");
        }

        config.Profiles.Remove(profileName);
        
        // Clear active profile if deleted
        if (config.CurrentProfile == profileName)
        {
            config.CurrentProfile = config.Profiles.Keys.FirstOrDefault() ?? string.Empty;
        }

        await SaveConfigAsync(config);
    }

    public async Task UpdatePreferencesAsync(string key, string value)
    {
        var config = await LoadConfigAsync();
        var activeProfile = await GetActiveProfileAsync();
        
        if (activeProfile == null)
        {
            throw new ValidationException("アクティブなプロファイルが設定されていません");
        }

        activeProfile.Preferences ??= new Preferences();
        
        switch (key.ToLower())
        {
            case "editor":
                activeProfile.Preferences.Editor = value;
                break;
            case "dateformat":
                activeProfile.Preferences.DateFormat = value;
                break;
            case "timeformat":
                activeProfile.Preferences.TimeFormat = value;
                break;
            case "pagesize":
                if (!int.TryParse(value, out var pageSize) || pageSize <= 0)
                {
                    throw new ValidationException("PageSize must be a positive integer");
                }
                activeProfile.Preferences.PageSize = pageSize;
                break;
            default:
                throw new ValidationException($"Unknown preference key: {key}");
        }

        config.Profiles[config.CurrentProfile!] = activeProfile;
        await SaveConfigAsync(config);
    }

    private string GetConfigPath()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            return _fileSystem.Path.Combine(appData, "redmine", "config.yml");
        }
        else
        {
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            return _fileSystem.Path.Combine(home, ".config", "redmine", "config.yml");
        }
    }

    private string EncryptApiKey(string apiKey)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            // Use DPAPI on Windows
            var userData = Encoding.UTF8.GetBytes(apiKey);
            var encryptedData = ProtectedData.Protect(userData, null, DataProtectionScope.CurrentUser);
            return Convert.ToBase64String(encryptedData);
        }
        else
        {
            // Simple base64 encoding for non-Windows platforms
            // In production, consider using a more secure method
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(apiKey));
        }
    }

    private string DecryptApiKey(string encryptedApiKey)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            // Use DPAPI on Windows
            var encryptedData = Convert.FromBase64String(encryptedApiKey);
            var decryptedData = ProtectedData.Unprotect(encryptedData, null, DataProtectionScope.CurrentUser);
            return Encoding.UTF8.GetString(decryptedData);
        }
        else
        {
            // Simple base64 decoding for non-Windows platforms
            return Encoding.UTF8.GetString(Convert.FromBase64String(encryptedApiKey));
        }
    }
}