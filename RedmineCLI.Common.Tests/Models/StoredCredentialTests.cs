using RedmineCLI.Common.Models;

namespace RedmineCLI.Common.Tests.Models;

public class StoredCredentialTests
{
    [Fact]
    public void Constructor_Should_InitializeProperties_When_Created()
    {
        // Arrange & Act
        var credential = new StoredCredential();

        // Assert
        credential.Username.Should().BeNull();
        credential.Password.Should().BeNull();
        credential.ApiKey.Should().BeNull();
        credential.SessionCookie.Should().BeNull();
        credential.SessionExpiry.Should().BeNull();
        credential.LastUpdated.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Properties_Should_BeSettable_When_Assigned()
    {
        // Arrange
        var credential = new StoredCredential();
        var now = DateTime.UtcNow;

        // Act
        credential.Username = "testuser";
        credential.Password = "testpass";
        credential.ApiKey = "testapikey";
        credential.SessionCookie = "testsession";
        credential.SessionExpiry = now.AddHours(1);
        credential.LastUpdated = now;

        // Assert
        credential.Username.Should().Be("testuser");
        credential.Password.Should().Be("testpass");
        credential.ApiKey.Should().Be("testapikey");
        credential.SessionCookie.Should().Be("testsession");
        credential.SessionExpiry.Should().Be(now.AddHours(1));
        credential.LastUpdated.Should().Be(now);
    }

    [Fact]
    public void HasValidSession_Should_ReturnTrue_When_SessionNotExpired()
    {
        // Arrange
        var credential = new StoredCredential
        {
            SessionCookie = "validsession",
            SessionExpiry = DateTime.UtcNow.AddHours(1)
        };

        // Act
        var result = credential.HasValidSession();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void HasValidSession_Should_ReturnFalse_When_SessionExpired()
    {
        // Arrange
        var credential = new StoredCredential
        {
            SessionCookie = "expiredsession",
            SessionExpiry = DateTime.UtcNow.AddHours(-1)
        };

        // Act
        var result = credential.HasValidSession();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void HasValidSession_Should_ReturnFalse_When_NoSession()
    {
        // Arrange
        var credential = new StoredCredential();

        // Act
        var result = credential.HasValidSession();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void HasPasswordCredentials_Should_ReturnTrue_When_UsernameAndPasswordSet()
    {
        // Arrange
        var credential = new StoredCredential
        {
            Username = "user",
            Password = "pass"
        };

        // Act
        var result = credential.HasPasswordCredentials();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void HasPasswordCredentials_Should_ReturnFalse_When_MissingUsername()
    {
        // Arrange
        var credential = new StoredCredential
        {
            Password = "pass"
        };

        // Act
        var result = credential.HasPasswordCredentials();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void HasPasswordCredentials_Should_ReturnFalse_When_MissingPassword()
    {
        // Arrange
        var credential = new StoredCredential
        {
            Username = "user"
        };

        // Act
        var result = credential.HasPasswordCredentials();

        // Assert
        result.Should().BeFalse();
    }
}
