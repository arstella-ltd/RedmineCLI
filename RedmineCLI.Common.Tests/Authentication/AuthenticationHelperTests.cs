using System.Net;
using System.Net.Http;
using System.Text;

using Microsoft.Extensions.Logging;

using NSubstitute;

using RedmineCLI.Common.Authentication;
using RedmineCLI.Common.Models;

using Xunit;

namespace RedmineCLI.Common.Tests.Authentication;

public class AuthenticationHelperTests
{
    private readonly ILogger _logger;

    public AuthenticationHelperTests()
    {
        _logger = Substitute.For<ILogger>();
    }

    [Fact]
    public async Task CreateSessionFromCredentialsAsync_WithValidSessionCookie_ReturnsExistingCookie()
    {
        // Arrange
        var redmineUrl = "https://redmine.example.com";
        var credential = new StoredCredential
        {
            SessionCookie = "_redmine_session=abc123",
            SessionExpiry = DateTime.UtcNow.AddDays(1)
        };

        // Act
        var result = await AuthenticationHelper.CreateSessionFromCredentialsAsync(redmineUrl, credential, _logger);

        // Assert
        Assert.Equal("_redmine_session=abc123", result);
        _logger.Received().LogDebug("Using existing valid session cookie");
    }

    [Fact]
    public async Task CreateSessionFromCredentialsAsync_WithExpiredSession_ReturnsNull()
    {
        // Arrange
        var redmineUrl = "https://redmine.example.com";
        var credential = new StoredCredential
        {
            SessionCookie = "_redmine_session=expired123",
            SessionExpiry = DateTime.UtcNow.AddDays(-1) // Expired
        };

        // Act
        var result = await AuthenticationHelper.CreateSessionFromCredentialsAsync(redmineUrl, credential, _logger);

        // Assert
        Assert.Null(result);
        _logger.Received().LogError("No valid credentials available");
    }

    [Fact]
    public async Task CreateSessionFromCredentialsAsync_WithOnlyApiKey_ReturnsNull()
    {
        // Arrange
        var redmineUrl = "https://redmine.example.com";
        var credential = new StoredCredential
        {
            ApiKey = "test-api-key-123"
        };

        // Act
        var result = await AuthenticationHelper.CreateSessionFromCredentialsAsync(redmineUrl, credential, _logger);

        // Assert
        Assert.Null(result);
        _logger.Received().LogWarning("Only API key available, cannot create session cookie");
    }

    [Fact]
    public async Task CreateSessionFromCredentialsAsync_WithNoCredentials_ReturnsNull()
    {
        // Arrange
        var redmineUrl = "https://redmine.example.com";
        var credential = new StoredCredential
        {
        };

        // Act
        var result = await AuthenticationHelper.CreateSessionFromCredentialsAsync(redmineUrl, credential, _logger);

        // Assert
        Assert.Null(result);
        _logger.Received().LogError("No valid credentials available");
    }

    [Fact]
    public async Task CreateSessionFromCredentialsAsync_WithNullLogger_DoesNotThrow()
    {
        // Arrange
        var redmineUrl = "https://redmine.example.com";
        var credential = new StoredCredential
        {
            SessionCookie = "_redmine_session=abc123",
            SessionExpiry = DateTime.UtcNow.AddDays(1)
        };

        // Act
        var result = await AuthenticationHelper.CreateSessionFromCredentialsAsync(redmineUrl, credential, null);

        // Assert
        Assert.Equal("_redmine_session=abc123", result);
    }
}
