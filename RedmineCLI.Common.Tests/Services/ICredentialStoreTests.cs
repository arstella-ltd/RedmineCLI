using RedmineCLI.Common.Models;
using RedmineCLI.Common.Services;

namespace RedmineCLI.Common.Tests.Services;

public class ICredentialStoreTests
{
    private readonly ICredentialStore _credentialStore;
    private readonly string _testServerUrl = "https://redmine.example.com";

    public ICredentialStoreTests()
    {
        _credentialStore = Substitute.For<ICredentialStore>();
    }

    [Fact]
    public async Task GetCredentialAsync_Should_ReturnCredential_When_Exists()
    {
        // Arrange
        var expectedCredential = new StoredCredential
        {
            Username = "testuser",
            Password = "testpass",
            LastUpdated = DateTime.UtcNow
        };
        _credentialStore.GetCredentialAsync(_testServerUrl)
            .Returns(Task.FromResult<StoredCredential?>(expectedCredential));

        // Act
        var result = await _credentialStore.GetCredentialAsync(_testServerUrl);

        // Assert
        result.Should().NotBeNull();
        result!.Username.Should().Be("testuser");
        result.Password.Should().Be("testpass");
    }

    [Fact]
    public async Task GetCredentialAsync_Should_ReturnNull_When_NotExists()
    {
        // Arrange
        _credentialStore.GetCredentialAsync(_testServerUrl)
            .Returns(Task.FromResult<StoredCredential?>(null));

        // Act
        var result = await _credentialStore.GetCredentialAsync(_testServerUrl);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task SaveCredentialAsync_Should_StoreCredential_When_Called()
    {
        // Arrange
        var credential = new StoredCredential
        {
            Username = "testuser",
            Password = "testpass",
            LastUpdated = DateTime.UtcNow
        };

        // Act
        await _credentialStore.SaveCredentialAsync(_testServerUrl, credential);

        // Assert
        await _credentialStore.Received(1).SaveCredentialAsync(_testServerUrl, credential);
    }

    [Fact]
    public async Task DeleteCredentialAsync_Should_RemoveCredential_When_Called()
    {
        // Arrange & Act
        await _credentialStore.DeleteCredentialAsync(_testServerUrl);

        // Assert
        await _credentialStore.Received(1).DeleteCredentialAsync(_testServerUrl);
    }

    [Fact]
    public async Task HasStoredPasswordAsync_Should_ReturnTrue_When_PasswordExists()
    {
        // Arrange
        _credentialStore.HasStoredPasswordAsync(_testServerUrl)
            .Returns(Task.FromResult(true));

        // Act
        var result = await _credentialStore.HasStoredPasswordAsync(_testServerUrl);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task HasStoredPasswordAsync_Should_ReturnFalse_When_NoPassword()
    {
        // Arrange
        _credentialStore.HasStoredPasswordAsync(_testServerUrl)
            .Returns(Task.FromResult(false));

        // Act
        var result = await _credentialStore.HasStoredPasswordAsync(_testServerUrl);

        // Assert
        result.Should().BeFalse();
    }
}
