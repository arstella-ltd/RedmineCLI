using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using VYaml.Annotations;
using VYaml.Serialization;

namespace RedmineCLI.Models;

[YamlObject]
public partial class Config
{
    public string CurrentProfile { get; set; } = "default";
    public Dictionary<string, Profile> Profiles { get; set; } = new();
    public Preferences Preferences { get; set; } = new();

    private static readonly byte[] _entropy = Encoding.UTF8.GetBytes("RedmineCLI");

    public void Save(string path)
    {
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        // Encrypt API keys before saving
        var configToSave = new Config
        {
            CurrentProfile = CurrentProfile,
            Profiles = new Dictionary<string, Profile>(),
            Preferences = Preferences
        };

        foreach (var kvp in Profiles)
        {
            var profile = kvp.Value;
            configToSave.Profiles[kvp.Key] = new Profile
            {
                Name = profile.Name,
                Url = profile.Url,
                ApiKey = string.IsNullOrEmpty(profile.ApiKey) 
                    ? string.Empty 
                    : EncryptApiKey(profile.ApiKey)
            };
        }

        var yaml = YamlSerializer.SerializeToString(configToSave);
        File.WriteAllText(path, yaml);
    }

    public static Config Load(string path)
    {
        if (!File.Exists(path))
        {
            return CreateDefaultConfig();
        }

        var yamlBytes = File.ReadAllBytes(path);
        var config = YamlSerializer.Deserialize<Config>(yamlBytes) ?? CreateDefaultConfig();

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

    public void Validate()
    {
        if (Profiles.Count == 0)
        {
            throw new ValidationException("At least one profile must be configured");
        }

        if (!Profiles.ContainsKey(CurrentProfile))
        {
            throw new ValidationException($"Current profile '{CurrentProfile}' does not exist");
        }
    }

    private static Config CreateDefaultConfig()
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

    private static string EncryptApiKey(string apiKey)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            try
            {
                var data = Encoding.UTF8.GetBytes(apiKey);
                var encrypted = ProtectedData.Protect(data, _entropy, DataProtectionScope.CurrentUser);
                return Convert.ToBase64String(encrypted);
            }
            catch
            {
                // Fallback to base64 encoding if encryption fails
                return Convert.ToBase64String(Encoding.UTF8.GetBytes(apiKey));
            }
        }
        else
        {
            // On non-Windows platforms, use base64 encoding
            // In a production scenario, you might want to use platform-specific key storage
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(apiKey));
        }
    }

    private static string DecryptApiKey(string encryptedApiKey)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            try
            {
                var encrypted = Convert.FromBase64String(encryptedApiKey);
                var data = ProtectedData.Unprotect(encrypted, _entropy, DataProtectionScope.CurrentUser);
                return Encoding.UTF8.GetString(data);
            }
            catch
            {
                // Fallback to base64 decoding if decryption fails
                try
                {
                    var data = Convert.FromBase64String(encryptedApiKey);
                    return Encoding.UTF8.GetString(data);
                }
                catch
                {
                    return encryptedApiKey; // Return as-is if all decryption attempts fail
                }
            }
        }
        else
        {
            // On non-Windows platforms, use base64 decoding
            try
            {
                var data = Convert.FromBase64String(encryptedApiKey);
                return Encoding.UTF8.GetString(data);
            }
            catch
            {
                return encryptedApiKey; // Return as-is if decoding fails
            }
        }
    }

}