using System.Runtime.InteropServices;

using NSubstitute;

using RedmineCLI.Common.Models;
using RedmineCLI.Common.Services;

using Xunit;

namespace RedmineCLI.Common.Tests.Services;

public class CredentialStoreTests
{
    [Fact]
    public void Create_OnWindows_ReturnsWindowsCredentialStore()
    {
        // このテストは実際のOSに依存するため、現在のプラットフォームをチェック
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            // Act
            var store = CredentialStore.Create();

            // Assert
            Assert.IsType<WindowsCredentialStore>(store);
        }
    }

    [Fact]
    public void Create_OnMacOS_ReturnsMacOSCredentialStore()
    {
        // このテストは実際のOSに依存するため、現在のプラットフォームをチェック
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            // Act
            var store = CredentialStore.Create();

            // Assert
            Assert.IsType<MacOSCredentialStore>(store);
        }
    }

    [Fact]
    public void Create_OnLinux_ReturnsAppropriatStore()
    {
        // このテストは実際のOSに依存するため、現在のプラットフォームをチェック
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            // Act
            var store = CredentialStore.Create();

            // Assert
            // Linux環境ではsecret-toolの有無によって異なる実装が返される
            Assert.True(store is LinuxCredentialStore || store is FileBasedCredentialStore);
        }
    }

    [Fact]
    public void Create_AlwaysReturnsNonNullStore()
    {
        // Act
        var store = CredentialStore.Create();

        // Assert
        Assert.NotNull(store);
        Assert.IsAssignableFrom<ICredentialStore>(store);
    }

    [Fact]
    public async Task HasStoredPasswordAsync_WithStoredPassword_ReturnsTrue()
    {
        // Arrange
        var mockStore = new TestCredentialStore();
        var serverUrl = "https://redmine.example.com";
        var credential = new StoredCredential
        {
            Username = "testuser",
            Password = "testpassword"
        };
        mockStore.TestCredentials[serverUrl] = credential;

        // Act
        var result = await mockStore.HasStoredPasswordAsync(serverUrl);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task HasStoredPasswordAsync_WithoutStoredPassword_ReturnsFalse()
    {
        // Arrange
        var mockStore = new TestCredentialStore();
        var serverUrl = "https://redmine.example.com";
        var credential = new StoredCredential
        {
            Username = "testuser",
            ApiKey = "api-key-only"
        };
        mockStore.TestCredentials[serverUrl] = credential;

        // Act
        var result = await mockStore.HasStoredPasswordAsync(serverUrl);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task HasStoredPasswordAsync_WithNoCredentials_ReturnsFalse()
    {
        // Arrange
        var mockStore = new TestCredentialStore();
        var serverUrl = "https://redmine.example.com";

        // Act
        var result = await mockStore.HasStoredPasswordAsync(serverUrl);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void GetKeyName_GeneratesCorrectKeyForUrl()
    {
        // Arrange & Act
        var key1 = TestCredentialStore.TestGetKeyName("https://redmine.example.com");
        var key2 = TestCredentialStore.TestGetKeyName("https://redmine.example.com:8080");
        var key3 = TestCredentialStore.TestGetKeyName("http://localhost:3000");

        // Assert
        Assert.Equal("RedmineCLI:redmine.example.com:443", key1);
        Assert.Equal("RedmineCLI:redmine.example.com:8080", key2);
        Assert.Equal("RedmineCLI:localhost:3000", key3);
    }

    [Fact]
    public void Dispose_CanBeCalledMultipleTimes()
    {
        // Arrange
        var store = new TestCredentialStore();

        // Act & Assert - should not throw
        store.Dispose();
        store.Dispose();
    }

    // テスト用の具象実装
    private class TestCredentialStore : CredentialStore
    {
        public Dictionary<string, StoredCredential> TestCredentials { get; } = new();

        public override Task<StoredCredential?> GetCredentialAsync(string serverUrl)
        {
            return Task.FromResult(TestCredentials.TryGetValue(serverUrl, out var cred) ? cred : null);
        }

        public override Task SaveCredentialAsync(string serverUrl, StoredCredential credential)
        {
            TestCredentials[serverUrl] = credential;
            return Task.CompletedTask;
        }

        public override Task DeleteCredentialAsync(string serverUrl)
        {
            TestCredentials.Remove(serverUrl);
            return Task.CompletedTask;
        }

        // 保護されたメソッドをテスト用に公開
        public static string TestGetKeyName(string serverUrl) => GetKeyName(serverUrl);
    }
}
