using System.IO;
using System.Runtime.InteropServices;

using RedmineCLI.Common.Models;
using RedmineCLI.Common.Services;

using Xunit;

namespace RedmineCLI.Common.Tests.Services;

public class FileBasedCredentialStoreTests : IDisposable
{
    private readonly string _testDirectory;
    private readonly FileBasedCredentialStore _store;

    public FileBasedCredentialStoreTests()
    {
        // テスト用の一時ディレクトリを作成
        _testDirectory = Path.Combine(Path.GetTempPath(), "redminetest_" + Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testDirectory);

        // 環境変数を設定してテストディレクトリを使用するように
        Environment.SetEnvironmentVariable("HOME", _testDirectory);
        Environment.SetEnvironmentVariable("USERPROFILE", _testDirectory);

        _store = new FileBasedCredentialStore();
    }

    [Fact]
    public async Task GetCredentialAsync_WhenFileDoesNotExist_ReturnsNull()
    {
        // Arrange
        var serverUrl = "https://redmine.example.com";

        // Act
        var result = await _store.GetCredentialAsync(serverUrl);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task SaveCredentialAsync_SavesCredentialToFile()
    {
        // Arrange
        var serverUrl = "https://redmine.example.com";
        var credential = new StoredCredential
        {
            Username = "testuser",
            Password = "testpass123",
            ApiKey = "api-key-123",
            SessionCookie = "_redmine_session=abc123",
            SessionExpiry = DateTime.UtcNow.AddDays(1)
        };

        // Act
        await _store.SaveCredentialAsync(serverUrl, credential);

        // Assert
        var savedCredential = await _store.GetCredentialAsync(serverUrl);
        Assert.NotNull(savedCredential);
        Assert.Equal(credential.Username, savedCredential.Username);
        Assert.Equal(credential.Password, savedCredential.Password);
        Assert.Equal(credential.ApiKey, savedCredential.ApiKey);
        Assert.Equal(credential.SessionCookie, savedCredential.SessionCookie);
    }

    [Fact]
    public async Task DeleteCredentialAsync_DeletesExistingCredential()
    {
        // Arrange
        var serverUrl = "https://redmine.example.com";
        var credential = new StoredCredential
        {
            Username = "testuser",
            Password = "testpass123"
        };
        await _store.SaveCredentialAsync(serverUrl, credential);

        // Act
        await _store.DeleteCredentialAsync(serverUrl);

        // Assert
        var result = await _store.GetCredentialAsync(serverUrl);
        Assert.Null(result);
    }

    [Fact]
    public async Task DeleteCredentialAsync_WhenFileDoesNotExist_DoesNotThrow()
    {
        // Arrange
        var serverUrl = "https://redmine.example.com";

        // Act & Assert
        await _store.DeleteCredentialAsync(serverUrl);
    }

    [Fact]
    public async Task GetCredentialAsync_WithCorruptedFile_ReturnsNull()
    {
        // Arrange
        var serverUrl = "https://redmine.example.com";
        var credentialsDir = Path.Combine(_testDirectory, ".config", "redmine", "credentials");
        Directory.CreateDirectory(credentialsDir);

        var filePath = Path.Combine(credentialsDir, "RedmineCLI_redmine.example.com_443.cred");
        await File.WriteAllTextAsync(filePath, "This is not valid base64!");

        // Act
        var result = await _store.GetCredentialAsync(serverUrl);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task SaveCredentialAsync_OverwritesExistingCredential()
    {
        // Arrange
        var serverUrl = "https://redmine.example.com";
        var credential1 = new StoredCredential
        {
            Username = "user1",
            Password = "pass1"
        };
        var credential2 = new StoredCredential
        {
            Username = "user2",
            Password = "pass2"
        };

        // Act
        await _store.SaveCredentialAsync(serverUrl, credential1);
        await _store.SaveCredentialAsync(serverUrl, credential2);

        // Assert
        var savedCredential = await _store.GetCredentialAsync(serverUrl);
        Assert.NotNull(savedCredential);
        Assert.Equal("user2", savedCredential.Username);
        Assert.Equal("pass2", savedCredential.Password);
    }

    [Fact]
    public async Task SaveCredentialAsync_HandlesSpecialCharactersInUrl()
    {
        // Arrange
        var serverUrl = "https://redmine.example.com:8080/path";
        var credential = new StoredCredential
        {
            Username = "testuser"
        };

        // Act
        await _store.SaveCredentialAsync(serverUrl, credential);

        // Assert
        var savedCredential = await _store.GetCredentialAsync(serverUrl);
        Assert.NotNull(savedCredential);
        Assert.Equal(credential.Username, savedCredential.Username);
    }

    [SkippableFact]
    public async Task SaveCredentialAsync_SetsCorrectFilePermissionsOnUnix()
    {
        // Skip this test on Windows
        Skip.If(RuntimeInformation.IsOSPlatform(OSPlatform.Windows), "This test is for Unix-like systems only");

        // Arrange
        var serverUrl = "https://redmine.example.com";
        var credential = new StoredCredential
        {
            Username = "testuser"
        };

        // Act
        await _store.SaveCredentialAsync(serverUrl, credential);

        // Assert
        var credentialsDir = Path.Combine(_testDirectory, ".config", "redmine", "credentials");
        var filePath = Path.Combine(credentialsDir, "RedmineCLI_redmine.example.com_443.cred");

        var fileInfo = new FileInfo(filePath);
        Assert.True(fileInfo.Exists);

        // Check file permissions (should be 600 - owner read/write only)
#pragma warning disable CA1416 // Validate platform compatibility
        var fileMode = File.GetUnixFileMode(filePath);
#pragma warning restore CA1416
        Assert.True(fileMode.HasFlag(UnixFileMode.UserRead));
        Assert.True(fileMode.HasFlag(UnixFileMode.UserWrite));
        Assert.False(fileMode.HasFlag(UnixFileMode.GroupRead));
        Assert.False(fileMode.HasFlag(UnixFileMode.OtherRead));
    }

    public void Dispose()
    {
        _store?.Dispose();

        // クリーンアップ
        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, true);
        }
    }
}
