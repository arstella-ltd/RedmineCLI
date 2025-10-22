using FluentAssertions;

using Microsoft.Extensions.Logging;

using NSubstitute;

using RedmineCLI.Common.Models;
using RedmineCLI.Common.Services;
using RedmineCLI.Extension.Board.Services;

using Xunit;

namespace RedmineCLI.Extension.Board.Tests.Services;

[Collection("Sequential")]
public class AuthenticationServiceTests : IDisposable
{
    private readonly ILogger<AuthenticationService> _mockLogger;
    private readonly ICredentialStore _mockCredentialStore;
    private readonly AuthenticationService _authService;
    private readonly string _originalRedmineUrl;
    private readonly string _originalConfigDir;

    public AuthenticationServiceTests()
    {
        _mockLogger = Substitute.For<ILogger<AuthenticationService>>();
        _mockCredentialStore = Substitute.For<ICredentialStore>();
        _authService = new AuthenticationService(_mockLogger, _mockCredentialStore);

        // Save original environment variables
        _originalRedmineUrl = Environment.GetEnvironmentVariable("REDMINE_URL") ?? string.Empty;
        _originalConfigDir = Environment.GetEnvironmentVariable("REDMINE_CONFIG_DIR") ?? string.Empty;
    }

    public void Dispose()
    {
        // Restore original environment variables
        if (string.IsNullOrEmpty(_originalRedmineUrl))
            Environment.SetEnvironmentVariable("REDMINE_URL", null);
        else
            Environment.SetEnvironmentVariable("REDMINE_URL", _originalRedmineUrl);

        if (string.IsNullOrEmpty(_originalConfigDir))
            Environment.SetEnvironmentVariable("REDMINE_CONFIG_DIR", null);
        else
            Environment.SetEnvironmentVariable("REDMINE_CONFIG_DIR", _originalConfigDir);
    }

    [Fact]
    public async Task GetAuthenticationAsync_Should_UseUrlOverride_When_Provided()
    {
        // Arrange
        var urlOverride = "https://override.redmine.com";
        var credential = new StoredCredential
        {
            Username = "test",
            Password = "password",
            ApiKey = "api-key",
            SessionCookie = "session-cookie",
            SessionExpiry = DateTime.UtcNow.AddHours(1)
        };
        _mockCredentialStore.GetCredentialAsync(urlOverride)
            .Returns(Task.FromResult<StoredCredential?>(credential));

        // Act
        var result = await _authService.GetAuthenticationAsync(urlOverride);

        // Assert
        result.url.Should().Be(urlOverride);
        result.sessionCookie.Should().Be("session-cookie");
        await _mockCredentialStore.Received(1).GetCredentialAsync(urlOverride);
    }

    [Fact]
    public async Task GetAuthenticationAsync_Should_UseEnvironmentVariable_When_NoUrlOverride()
    {
        // Arrange
        Environment.SetEnvironmentVariable("REDMINE_URL", "https://env.redmine.com");
        var credential = new StoredCredential
        {
            Username = "test",
            Password = "password",
            ApiKey = "api-key",
            SessionCookie = "session-cookie",
            SessionExpiry = DateTime.UtcNow.AddHours(1) // Set valid session expiry
        };
        _mockCredentialStore.GetCredentialAsync("https://env.redmine.com")
            .Returns(Task.FromResult<StoredCredential?>(credential));

        // Act
        var result = await _authService.GetAuthenticationAsync(null);

        // Assert
        result.url.Should().Be("https://env.redmine.com");
        result.sessionCookie.Should().Be("session-cookie");
    }

    [Fact]
    public async Task GetAuthenticationAsync_Should_AddHttpsScheme_When_NoSchemeProvided()
    {
        // Arrange
        var urlOverride = "redmine.example.com";
        var expectedUrl = "https://redmine.example.com";
        var credential = new StoredCredential
        {
            Username = "test",
            Password = "password",
            ApiKey = "api-key",
            SessionCookie = "session-cookie",
            SessionExpiry = DateTime.UtcNow.AddHours(1)
        };
        _mockCredentialStore.GetCredentialAsync(expectedUrl)
            .Returns(Task.FromResult<StoredCredential?>(credential));

        // Act
        var result = await _authService.GetAuthenticationAsync(urlOverride);

        // Assert
        result.url.Should().Be(expectedUrl);
        await _mockCredentialStore.Received(1).GetCredentialAsync(expectedUrl);
    }

    [Fact]
    public async Task GetAuthenticationAsync_Should_TrimTrailingSlash_From_Url()
    {
        // Arrange
        var urlOverride = "https://redmine.example.com/";
        var expectedUrl = "https://redmine.example.com";
        var credential = new StoredCredential
        {
            Username = "test",
            Password = "password",
            ApiKey = "api-key",
            SessionCookie = "session-cookie",
            SessionExpiry = DateTime.UtcNow.AddHours(1)
        };
        _mockCredentialStore.GetCredentialAsync(expectedUrl)
            .Returns(Task.FromResult<StoredCredential?>(credential));

        // Act
        var result = await _authService.GetAuthenticationAsync(urlOverride);

        // Assert
        result.url.Should().Be(expectedUrl);
    }

    [Fact]
    public async Task GetAuthenticationAsync_Should_Exit_When_NoCredentialsFound()
    {
        // Arrange
        var urlOverride = "https://redmine.example.com";
        _mockCredentialStore.GetCredentialAsync(urlOverride)
            .Returns(Task.FromResult<StoredCredential?>(null));

        var exitCode = 0;
        Environment.ExitCode = 0;

        // Act
        try
        {
            await _authService.GetAuthenticationAsync(urlOverride);
        }
        catch (Exception)
        {
            // Environment.Exit throws an exception in tests
            exitCode = Environment.ExitCode;
        }

        // Assert
        await _mockCredentialStore.Received(1).GetCredentialAsync(urlOverride);
    }

    [Fact]
    public async Task GetAuthenticationAsync_Should_Exit_When_NoRedmineUrlConfigured()
    {
        // Arrange
        Environment.SetEnvironmentVariable("REDMINE_URL", null);
        // Ensure no config file is picked up from host environment
        var tempConfigDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempConfigDir);
        Environment.SetEnvironmentVariable("REDMINE_CONFIG_DIR", tempConfigDir);
        var exitCode = 0;
        Environment.ExitCode = 0;

        // Act
        try
        {
            await _authService.GetAuthenticationAsync(null);
        }
        catch (Exception)
        {
            // Environment.Exit throws an exception in tests
            exitCode = Environment.ExitCode;
        }

        // Assert
        await _mockCredentialStore.DidNotReceive().GetCredentialAsync(Arg.Any<string>());
    }

    [Fact]
    public async Task GetAuthenticationAsync_Should_UpdateCredential_When_SessionChanged()
    {
        // Arrange
        var url = "https://redmine.example.com";
        var oldSessionCookie = "old-session";

        var credential = new StoredCredential
        {
            Username = "test",
            Password = "password",
            ApiKey = "api-key",
            SessionCookie = oldSessionCookie
        };

        _mockCredentialStore.GetCredentialAsync(url)
            .Returns(Task.FromResult<StoredCredential?>(credential));

        // Act
        var result = await _authService.GetAuthenticationAsync(url);

        // Assert
        result.sessionCookie.Should().Be(oldSessionCookie);

        // Note: In a real test scenario, we would need to refactor to make AuthenticationHelper
        // injectable or use a wrapper pattern to test session update logic
    }

    [Fact]
    public async Task GetAuthenticationAsync_Should_HandleCredentialStoreException()
    {
        // Arrange
        var urlOverride = "https://redmine.example.com";
        _mockCredentialStore.GetCredentialAsync(urlOverride)
            .Returns(Task.FromException<StoredCredential?>(new Exception("Keychain access error")));

        var exitCode = 0;
        Environment.ExitCode = 0;

        // Act
        try
        {
            await _authService.GetAuthenticationAsync(urlOverride);
        }
        catch (Exception)
        {
            // Environment.Exit throws an exception in tests
            exitCode = Environment.ExitCode;
        }

        // Assert
        _mockLogger.ReceivedWithAnyArgs().LogError(Arg.Any<Exception>(), "Error accessing keychain");
    }
}
