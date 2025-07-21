using FluentAssertions;
using RedmineCLI.Exceptions;
using RedmineCLI.Models;
using System.IO;

namespace RedmineCLI.Tests.Models;

public class ConfigTests
{
    private readonly string _testConfigPath;

    public ConfigTests()
    {
        _testConfigPath = Path.Combine(Path.GetTempPath(), $"test-config-{Guid.NewGuid()}.yml");
    }

    [Fact]
    public void Save_Should_PersistSettings_When_ValidConfigProvided()
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
                    ApiKey = "test-api-key-123"
                }
            },
            Preferences = new Preferences
            {
                DefaultFormat = "table",
                PageSize = 25
            }
        };

        // Act
        config.Save(_testConfigPath);

        // Assert
        File.Exists(_testConfigPath).Should().BeTrue();
        
        var loadedConfig = Config.Load(_testConfigPath);
        loadedConfig.CurrentProfile.Should().Be("default");
        loadedConfig.Profiles.Should().ContainKey("default");
        loadedConfig.Profiles["default"].Url.Should().Be("https://redmine.example.com");
        loadedConfig.Preferences.DefaultFormat.Should().Be("table");
        loadedConfig.Preferences.PageSize.Should().Be(25);

        // Cleanup
        File.Delete(_testConfigPath);
    }

    [Fact]
    public void Load_Should_ReturnDefaultConfig_When_FileNotExists()
    {
        // Arrange
        var nonExistentPath = Path.Combine(Path.GetTempPath(), "non-existent-config.yml");

        // Act
        var config = Config.Load(nonExistentPath);

        // Assert
        config.Should().NotBeNull();
        config.CurrentProfile.Should().Be("default");
        config.Profiles.Should().NotBeNull();
        config.Profiles.Should().ContainKey("default");
        config.Preferences.Should().NotBeNull();
        config.Preferences.DefaultFormat.Should().Be("table");
        config.Preferences.PageSize.Should().Be(20);
    }

    [Fact]
    public void ApiKey_Should_BeEncrypted_When_Saved()
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
                    ApiKey = "plain-text-api-key"
                }
            }
        };

        // Act
        config.Save(_testConfigPath);

        // Assert
        var fileContent = File.ReadAllText(_testConfigPath);
        fileContent.Should().NotContain("plain-text-api-key");
        
        // Verify decryption works
        var loadedConfig = Config.Load(_testConfigPath);
        loadedConfig.Profiles["default"].ApiKey.Should().Be("plain-text-api-key");

        // Cleanup
        File.Delete(_testConfigPath);
    }

    [Fact]
    public void Validate_Should_ThrowException_When_ProfilesAreEmpty()
    {
        // Arrange
        var config = new Config
        {
            CurrentProfile = "default",
            Profiles = new Dictionary<string, Profile>()
        };

        // Act
        var act = () => config.Validate();

        // Assert
        act.Should().Throw<ValidationException>()
            .WithMessage("At least one profile must be configured");
    }

    [Fact]
    public void Validate_Should_ThrowException_When_CurrentProfileNotExists()
    {
        // Arrange
        var config = new Config
        {
            CurrentProfile = "non-existent",
            Profiles = new Dictionary<string, Profile>
            {
                ["default"] = new Profile { Name = "default" }
            }
        };

        // Act
        var act = () => config.Validate();

        // Assert
        act.Should().Throw<ValidationException>()
            .WithMessage("Current profile 'non-existent' does not exist");
    }
}