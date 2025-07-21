using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;

using FluentAssertions;

using RedmineCLI.Exceptions;
using RedmineCLI.Models;
using RedmineCLI.Services;

using VYaml.Serialization;

namespace RedmineCLI.Tests.Services;

public class ConfigServiceTests
{
    private readonly MockFileSystem _fileSystem;
    private readonly ConfigService _configService;
    private readonly string _configPath;

    public ConfigServiceTests()
    {
        _fileSystem = new MockFileSystem();
        _configService = new ConfigService(_fileSystem);

        // Setup config path based on OS
        _configPath = Environment.OSVersion.Platform == PlatformID.Win32NT
            ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "redmine", "config.yml")
            : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".config", "redmine", "config.yml");
    }

    [Fact]
    public async Task LoadConfig_Should_ReturnDefaultConfig_When_FileNotExists()
    {
        // Arrange
        // File does not exist in MockFileSystem

        // Act
        var config = await _configService.LoadConfigAsync();

        // Assert
        config.Should().NotBeNull();
        config.CurrentProfile.Should().Be("default");
        config.Profiles.Should().ContainKey("default");
        config.Profiles["default"].Name.Should().Be("default");
        config.Preferences.Should().NotBeNull();
    }

    [Fact]
    public async Task SaveConfig_Should_CreateFile_When_FirstTimeSave()
    {
        // Arrange
        var config = new Config
        {
            CurrentProfile = "default",
            Profiles = new Dictionary<string, Profile>
            {
                ["default"] = new Profile
                {
                    Name = "default",
                    Url = "https://redmine.example.com",
                    ApiKey = "test-api-key",
                    DefaultProject = "test-project"
                }
            }
        };

        // Act
        await _configService.SaveConfigAsync(config);

        // Assert
        _fileSystem.File.Exists(_configPath).Should().BeTrue();
        var savedContent = await _fileSystem.File.ReadAllTextAsync(_configPath);
        savedContent.Should().NotBeNullOrEmpty();

        // Verify the file contains expected content (YAML format)
        savedContent.Should().Contain("currentProfile: default");
        savedContent.Should().Contain("profiles:");
        savedContent.Should().Contain("name: default");
        savedContent.Should().Contain("url: \"https://redmine.example.com\"");
    }

    [Fact]
    public async Task LoadConfig_Should_ThrowException_When_FileIsCorrupted()
    {
        // Arrange
        var corruptedYaml = "invalid yaml content { ] [";
        _fileSystem.Directory.CreateDirectory(Path.GetDirectoryName(_configPath)!);
        await _fileSystem.File.WriteAllTextAsync(_configPath, corruptedYaml);

        // Act
        var act = async () => await _configService.LoadConfigAsync();

        // Assert
        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("設定ファイルの読み込みに失敗しました*");
    }

    [Fact]
    public async Task SwitchProfile_Should_ChangeCurrentProfile_When_ProfileExists()
    {
        // Arrange
        var config = new Config
        {
            CurrentProfile = "default",
            Profiles = new Dictionary<string, Profile>
            {
                ["default"] = new Profile { Name = "default", Url = "https://redmine1.com", ApiKey = "key1" },
                ["production"] = new Profile { Name = "production", Url = "https://redmine2.com", ApiKey = "key2" }
            }
        };
        await _configService.SaveConfigAsync(config);

        // Act
        await _configService.SwitchProfileAsync("production");

        // Assert
        var updatedConfig = await _configService.LoadConfigAsync();
        updatedConfig.CurrentProfile.Should().Be("production");
    }

    [Fact]
    public async Task CreateProfile_Should_AddNewProfile_When_NameIsUnique()
    {
        // Arrange
        var config = new Config
        {
            CurrentProfile = "default",
            Profiles = new Dictionary<string, Profile>
            {
                ["default"] = new Profile { Name = "default", Url = "https://redmine1.com", ApiKey = "key1" }
            }
        };
        await _configService.SaveConfigAsync(config);

        var newProfile = new Profile
        {
            Name = "staging",
            Url = "https://staging.redmine.com",
            ApiKey = "staging-key",
            DefaultProject = "staging-project"
        };

        // Act
        await _configService.CreateProfileAsync(newProfile);

        // Assert
        var updatedConfig = await _configService.LoadConfigAsync();
        updatedConfig.Profiles.Should().ContainKey("staging");
        updatedConfig.Profiles["staging"].Should().BeEquivalentTo(newProfile);
    }

    [Fact]
    public async Task CreateProfile_Should_ThrowException_When_NameAlreadyExists()
    {
        // Arrange
        var config = new Config
        {
            CurrentProfile = "default",
            Profiles = new Dictionary<string, Profile>
            {
                ["default"] = new Profile { Name = "default", Url = "https://redmine1.com", ApiKey = "key1" }
            }
        };
        await _configService.SaveConfigAsync(config);

        var duplicateProfile = new Profile
        {
            Name = "default",
            Url = "https://another.redmine.com",
            ApiKey = "another-key"
        };

        // Act
        var act = async () => await _configService.CreateProfileAsync(duplicateProfile);

        // Assert
        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("プロファイル 'default' は既に存在します");
    }

    [Fact]
    public async Task EncryptApiKey_Should_NotStorePlainText_When_Saving()
    {
        // Arrange
        var plainApiKey = "my-secret-api-key";
        var config = new Config
        {
            CurrentProfile = "secure",
            Profiles = new Dictionary<string, Profile>
            {
                ["secure"] = new Profile
                {
                    Name = "secure",
                    Url = "https://secure.redmine.com",
                    ApiKey = plainApiKey
                }
            }
        };

        // Act
        await _configService.SaveConfigAsync(config);

        // Assert
        var savedContent = await _fileSystem.File.ReadAllTextAsync(_configPath);

        if (Environment.OSVersion.Platform == PlatformID.Win32NT)
        {
            // On Windows, API key should be encrypted
            savedContent.Should().NotContain(plainApiKey);
            savedContent.Should().Contain("apiKey:"); // But should have an apiKey field
        }
        else
        {
            // On non-Windows, API key is base64 encoded
            savedContent.Should().NotContain(plainApiKey);
            var base64Key = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(plainApiKey));
            savedContent.Should().Contain(base64Key);
        }
    }

    [Fact]
    public async Task DecryptApiKey_Should_ReturnOriginalValue_When_Loading()
    {
        // Arrange
        var originalApiKey = "my-secret-api-key";
        var config = new Config
        {
            CurrentProfile = "secure",
            Profiles = new Dictionary<string, Profile>
            {
                ["secure"] = new Profile
                {
                    Name = "secure",
                    Url = "https://secure.redmine.com",
                    ApiKey = originalApiKey
                }
            }
        };

        // Act
        await _configService.SaveConfigAsync(config);
        var loadedConfig = await _configService.LoadConfigAsync();

        // Assert
        loadedConfig.Profiles["secure"].ApiKey.Should().Be(originalApiKey);
    }

    [Fact]
    public async Task GetActiveProfile_Should_ReturnActiveProfile_When_ProfileExists()
    {
        // Arrange
        var config = new Config
        {
            CurrentProfile = "production",
            Profiles = new Dictionary<string, Profile>
            {
                ["default"] = new Profile { Name = "default", Url = "https://default.redmine.com", ApiKey = "default-key" },
                ["production"] = new Profile { Name = "production", Url = "https://prod.redmine.com", ApiKey = "prod-key" }
            }
        };
        await _configService.SaveConfigAsync(config);

        // Act
        var activeProfile = await _configService.GetActiveProfileAsync();

        // Assert
        activeProfile.Should().NotBeNull();
        activeProfile!.Name.Should().Be("production");
        activeProfile.Url.Should().Be("https://prod.redmine.com");
    }

    [Fact]
    public async Task GetActiveProfile_Should_ReturnNull_When_NoCurrentProfile()
    {
        // Arrange
        var config = new Config
        {
            CurrentProfile = "",
            Profiles = new Dictionary<string, Profile>
            {
                ["default"] = new Profile { Name = "default", Url = "https://default.redmine.com", ApiKey = "default-key" }
            }
        };
        await _configService.SaveConfigAsync(config);

        // Act
        var activeProfile = await _configService.GetActiveProfileAsync();

        // Assert
        activeProfile.Should().BeNull();
    }

    [Fact]
    public async Task GetActiveProfile_Should_ReturnNull_When_CurrentProfileNotFound()
    {
        // Arrange
        var config = new Config
        {
            CurrentProfile = "nonexistent",
            Profiles = new Dictionary<string, Profile>
            {
                ["default"] = new Profile { Name = "default", Url = "https://default.redmine.com", ApiKey = "default-key" }
            }
        };
        await _configService.SaveConfigAsync(config);

        // Act
        var activeProfile = await _configService.GetActiveProfileAsync();

        // Assert
        activeProfile.Should().BeNull();
    }

    [Fact]
    public async Task DeleteProfile_Should_RemoveProfile_When_ProfileExists()
    {
        // Arrange
        var config = new Config
        {
            CurrentProfile = "default",
            Profiles = new Dictionary<string, Profile>
            {
                ["default"] = new Profile { Name = "default", Url = "https://default.redmine.com", ApiKey = "default-key" },
                ["production"] = new Profile { Name = "production", Url = "https://prod.redmine.com", ApiKey = "prod-key" }
            }
        };
        await _configService.SaveConfigAsync(config);

        // Act
        await _configService.DeleteProfileAsync("production");

        // Assert
        var updatedConfig = await _configService.LoadConfigAsync();
        updatedConfig.Profiles.Should().NotContainKey("production");
        updatedConfig.Profiles.Should().ContainKey("default");
    }

    [Fact]
    public async Task DeleteProfile_Should_UpdateCurrentProfile_When_DeletingActiveProfile()
    {
        // Arrange
        var config = new Config
        {
            CurrentProfile = "production",
            Profiles = new Dictionary<string, Profile>
            {
                ["default"] = new Profile { Name = "default", Url = "https://default.redmine.com", ApiKey = "default-key" },
                ["production"] = new Profile { Name = "production", Url = "https://prod.redmine.com", ApiKey = "prod-key" }
            }
        };
        await _configService.SaveConfigAsync(config);

        // Act
        await _configService.DeleteProfileAsync("production");

        // Assert
        var updatedConfig = await _configService.LoadConfigAsync();
        updatedConfig.CurrentProfile.Should().Be("default");
        updatedConfig.Profiles.Should().NotContainKey("production");
    }

    [Fact]
    public async Task DeleteProfile_Should_ClearCurrentProfile_When_DeletingLastProfile()
    {
        // Arrange
        var config = new Config
        {
            CurrentProfile = "default",
            Profiles = new Dictionary<string, Profile>
            {
                ["default"] = new Profile { Name = "default", Url = "https://default.redmine.com", ApiKey = "default-key" }
            }
        };
        await _configService.SaveConfigAsync(config);

        // Act
        await _configService.DeleteProfileAsync("default");

        // Assert
        var updatedConfig = await _configService.LoadConfigAsync();
        updatedConfig.CurrentProfile.Should().BeEmpty();
        updatedConfig.Profiles.Should().BeEmpty();
    }

    [Fact]
    public async Task DeleteProfile_Should_ThrowException_When_ProfileNotFound()
    {
        // Arrange
        var config = new Config
        {
            CurrentProfile = "default",
            Profiles = new Dictionary<string, Profile>
            {
                ["default"] = new Profile { Name = "default", Url = "https://default.redmine.com", ApiKey = "default-key" }
            }
        };
        await _configService.SaveConfigAsync(config);

        // Act
        var act = async () => await _configService.DeleteProfileAsync("nonexistent");

        // Assert
        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("プロファイル 'nonexistent' が見つかりません");
    }

    [Fact]
    public async Task UpdatePreferences_Should_UpdateEditor_When_KeyIsEditor()
    {
        // Arrange
        var config = new Config
        {
            CurrentProfile = "default",
            Profiles = new Dictionary<string, Profile>
            {
                ["default"] = new Profile
                {
                    Name = "default",
                    Url = "https://default.redmine.com",
                    ApiKey = "default-key",
                    Preferences = new Preferences()
                }
            }
        };
        await _configService.SaveConfigAsync(config);

        // Act
        await _configService.UpdatePreferencesAsync("editor", "vim");

        // Assert
        var updatedConfig = await _configService.LoadConfigAsync();
        updatedConfig.Profiles["default"].Preferences!.Editor.Should().Be("vim");
    }

    [Fact]
    public async Task UpdatePreferences_Should_UpdateDateFormat_When_KeyIsDateFormat()
    {
        // Arrange
        var config = new Config
        {
            CurrentProfile = "default",
            Profiles = new Dictionary<string, Profile>
            {
                ["default"] = new Profile
                {
                    Name = "default",
                    Url = "https://default.redmine.com",
                    ApiKey = "default-key",
                    Preferences = new Preferences()
                }
            }
        };
        await _configService.SaveConfigAsync(config);

        // Act
        await _configService.UpdatePreferencesAsync("dateformat", "yyyy-MM-dd");

        // Assert
        var updatedConfig = await _configService.LoadConfigAsync();
        updatedConfig.Profiles["default"].Preferences!.DateFormat.Should().Be("yyyy-MM-dd");
    }

    [Fact]
    public async Task UpdatePreferences_Should_UpdateTimeFormat_When_KeyIsTimeFormat()
    {
        // Arrange
        var config = new Config
        {
            CurrentProfile = "default",
            Profiles = new Dictionary<string, Profile>
            {
                ["default"] = new Profile
                {
                    Name = "default",
                    Url = "https://default.redmine.com",
                    ApiKey = "default-key",
                    Preferences = new Preferences()
                }
            }
        };
        await _configService.SaveConfigAsync(config);

        // Act
        await _configService.UpdatePreferencesAsync("timeformat", "HH:mm:ss");

        // Assert
        var updatedConfig = await _configService.LoadConfigAsync();
        updatedConfig.Profiles["default"].Preferences!.TimeFormat.Should().Be("HH:mm:ss");
    }

    [Fact]
    public async Task UpdatePreferences_Should_UpdatePageSize_When_KeyIsPageSize()
    {
        // Arrange
        var config = new Config
        {
            CurrentProfile = "default",
            Profiles = new Dictionary<string, Profile>
            {
                ["default"] = new Profile
                {
                    Name = "default",
                    Url = "https://default.redmine.com",
                    ApiKey = "default-key",
                    Preferences = new Preferences()
                }
            }
        };
        await _configService.SaveConfigAsync(config);

        // Act
        await _configService.UpdatePreferencesAsync("pagesize", "50");

        // Assert
        var updatedConfig = await _configService.LoadConfigAsync();
        updatedConfig.Profiles["default"].Preferences!.PageSize.Should().Be(50);
    }

    [Fact]
    public async Task UpdatePreferences_Should_ThrowException_When_PageSizeIsInvalid()
    {
        // Arrange
        var config = new Config
        {
            CurrentProfile = "default",
            Profiles = new Dictionary<string, Profile>
            {
                ["default"] = new Profile
                {
                    Name = "default",
                    Url = "https://default.redmine.com",
                    ApiKey = "default-key",
                    Preferences = new Preferences()
                }
            }
        };
        await _configService.SaveConfigAsync(config);

        // Act
        var act = async () => await _configService.UpdatePreferencesAsync("pagesize", "invalid");

        // Assert
        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("PageSize must be a positive integer");
    }

    [Fact]
    public async Task UpdatePreferences_Should_ThrowException_When_PageSizeIsNegative()
    {
        // Arrange
        var config = new Config
        {
            CurrentProfile = "default",
            Profiles = new Dictionary<string, Profile>
            {
                ["default"] = new Profile
                {
                    Name = "default",
                    Url = "https://default.redmine.com",
                    ApiKey = "default-key",
                    Preferences = new Preferences()
                }
            }
        };
        await _configService.SaveConfigAsync(config);

        // Act
        var act = async () => await _configService.UpdatePreferencesAsync("pagesize", "-10");

        // Assert
        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("PageSize must be a positive integer");
    }

    [Fact]
    public async Task UpdatePreferences_Should_ThrowException_When_UnknownKey()
    {
        // Arrange
        var config = new Config
        {
            CurrentProfile = "default",
            Profiles = new Dictionary<string, Profile>
            {
                ["default"] = new Profile
                {
                    Name = "default",
                    Url = "https://default.redmine.com",
                    ApiKey = "default-key",
                    Preferences = new Preferences()
                }
            }
        };
        await _configService.SaveConfigAsync(config);

        // Act
        var act = async () => await _configService.UpdatePreferencesAsync("unknownkey", "value");

        // Assert
        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("Unknown preference key: unknownkey");
    }

    [Fact]
    public async Task UpdatePreferences_Should_ThrowException_When_NoActiveProfile()
    {
        // Arrange
        var config = new Config
        {
            CurrentProfile = "",
            Profiles = new Dictionary<string, Profile>()
        };
        await _configService.SaveConfigAsync(config);

        // Act
        var act = async () => await _configService.UpdatePreferencesAsync("editor", "vim");

        // Assert
        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("アクティブなプロファイルが設定されていません");
    }

    [Fact]
    public async Task UpdatePreferences_Should_CreatePreferences_When_PreferencesIsNull()
    {
        // Arrange
        var config = new Config
        {
            CurrentProfile = "default",
            Profiles = new Dictionary<string, Profile>
            {
                ["default"] = new Profile
                {
                    Name = "default",
                    Url = "https://default.redmine.com",
                    ApiKey = "default-key",
                    Preferences = null
                }
            }
        };
        await _configService.SaveConfigAsync(config);

        // Act
        await _configService.UpdatePreferencesAsync("editor", "nano");

        // Assert
        var updatedConfig = await _configService.LoadConfigAsync();
        updatedConfig.Profiles["default"].Preferences.Should().NotBeNull();
        updatedConfig.Profiles["default"].Preferences!.Editor.Should().Be("nano");
    }

    [Fact]
    public async Task SaveConfig_Should_ThrowException_When_WritePermissionDenied()
    {
        // Arrange
        var config = new Config
        {
            CurrentProfile = "default",
            Profiles = new Dictionary<string, Profile>
            {
                ["default"] = new Profile { Name = "default", Url = "https://test.com", ApiKey = "key" }
            }
        };

        // Simulate permission denied by making the file read-only
        var directory = Path.GetDirectoryName(_configPath)!;
        _fileSystem.Directory.CreateDirectory(directory);
        _fileSystem.File.Create(_configPath).Close();
        _fileSystem.File.SetAttributes(_configPath, FileAttributes.ReadOnly);

        // Act
        var act = async () => await _configService.SaveConfigAsync(config);

        // Assert
        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("設定ファイルの保存に失敗しました*");
    }

    [Fact]
    public async Task CreateProfile_Should_SetAsCurrentProfile_When_FirstProfile()
    {
        // Arrange
        var emptyConfig = new Config
        {
            CurrentProfile = "",
            Profiles = new Dictionary<string, Profile>(),
            Preferences = new Preferences()
        };
        await _configService.SaveConfigAsync(emptyConfig);

        var newProfile = new Profile
        {
            Name = "first-profile",
            Url = "https://first.redmine.com",
            ApiKey = "first-key"
        };

        // Act
        await _configService.CreateProfileAsync(newProfile);

        // Assert
        var updatedConfig = await _configService.LoadConfigAsync();
        updatedConfig.CurrentProfile.Should().Be("first-profile");
        updatedConfig.Profiles.Should().ContainKey("first-profile");
    }

    [Fact]
    public async Task SwitchProfile_Should_ThrowException_When_ProfileNotFound()
    {
        // Arrange
        var config = new Config
        {
            CurrentProfile = "default",
            Profiles = new Dictionary<string, Profile>
            {
                ["default"] = new Profile { Name = "default", Url = "https://test.com", ApiKey = "key" }
            }
        };
        await _configService.SaveConfigAsync(config);

        // Act
        var act = async () => await _configService.SwitchProfileAsync("nonexistent");

        // Assert
        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("プロファイル 'nonexistent' が見つかりません");
    }

    [Fact]
    public async Task CreateProfile_Should_ThrowException_When_NameIsEmpty()
    {
        // Arrange
        var profileWithEmptyName = new Profile
        {
            Name = "",
            Url = "https://test.com",
            ApiKey = "key"
        };

        // Act
        var act = async () => await _configService.CreateProfileAsync(profileWithEmptyName);

        // Assert
        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("プロファイル名は必須です");
    }
}
