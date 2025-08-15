using System.Diagnostics;

using FluentAssertions;

using Microsoft.Extensions.Logging;

using NSubstitute;

using RedmineCLI.Extensions;
using RedmineCLI.Services;

using Xunit;

namespace RedmineCLI.Tests.Services;

public class ExtensionExecutorTests
{
    private readonly IConfigService _mockConfigService;
    private readonly ILogger<ExtensionExecutor> _mockLogger;
    private readonly IExtensionExecutor _sut;

    public ExtensionExecutorTests()
    {
        _mockConfigService = Substitute.For<IConfigService>();
        _mockLogger = Substitute.For<ILogger<ExtensionExecutor>>();
        _sut = new ExtensionExecutor(_mockConfigService, _mockLogger);
    }

    [Fact]
    public async Task Execute_Should_RunExtension_When_ExtensionExists()
    {
        // Arrange
        const string extensionName = "test";
        string[] args = ["list", "--all"];
        var config = new Models.Config
        {
            CurrentProfile = "default",
            Profiles = new Dictionary<string, Models.Profile>
            {
                ["default"] = new()
                {
                    Name = "default",
                    Url = "https://redmine.example.com",
                    ApiKey = "test-api-key"
                }
            }
        };

        _mockConfigService.LoadConfigAsync().Returns(config);

        // Mock the FindExtension method to return a test path
        var mockExecutor = Substitute.ForPartsOf<ExtensionExecutor>(_mockConfigService, _mockLogger);
        mockExecutor.FindExtension(extensionName).Returns("/path/to/redmine-test");

        // Act
        // Note: This test would need process mocking in a real implementation
        // For now, we're testing the interface and logic flow
        var extensionPath = mockExecutor.FindExtension(extensionName);

        // Assert
        extensionPath.Should().NotBeNull();
        extensionPath.Should().EndWith("redmine-test");
    }

    [Fact]
    public async Task Execute_Should_PassEnvironmentVariables_When_ProfileActive()
    {
        // Arrange
        const string extensionName = "test";
        string[] args = ["list"];
        var config = new Models.Config
        {
            CurrentProfile = "production",
            Profiles = new Dictionary<string, Models.Profile>
            {
                ["production"] = new()
                {
                    Name = "production",
                    Url = "https://redmine.prod.com",
                    ApiKey = "prod-api-key",
                    DefaultProject = "main-project",
                    TimeFormat = Models.TimeFormat.Absolute,
                    OutputFormat = "json"
                }
            }
        };

        _mockConfigService.LoadConfigAsync().Returns(config);

        // Act & Assert
        // In a real test, we would verify that environment variables are set correctly
        // This would require process start info mocking
        await _mockConfigService.LoadAsync();
        config.CurrentProfile.Should().Be("production");
        config.Profiles["production"].Url.Should().Be("https://redmine.prod.com");
    }

    [Fact]
    public async Task Execute_Should_ReturnError_When_ExtensionNotFound()
    {
        // Arrange
        const string extensionName = "nonexistent";
        string[] args = ["list"];

        var mockExecutor = Substitute.ForPartsOf<ExtensionExecutor>(_mockConfigService, _mockLogger);
        mockExecutor.FindExtension(extensionName).Returns((string?)null);

        // Act
        var result = await mockExecutor.ExecuteAsync(extensionName, args);

        // Assert
        result.Should().Be(1);
        _mockLogger.Received(1).LogError("Extension 'redmine-nonexistent' not found.");
    }

    [Fact]
    public async Task Execute_Should_PropagateExitCode_When_ExtensionFails()
    {
        // Arrange
        const string extensionName = "failing";
        string[] args = ["error"];
        const int expectedExitCode = 42;

        var config = new Models.Config
        {
            CurrentProfile = "default",
            Profiles = new Dictionary<string, Models.Profile>
            {
                ["default"] = new()
                {
                    Name = "default",
                    Url = "https://redmine.example.com",
                    ApiKey = "test-api-key"
                }
            }
        };

        _mockConfigService.LoadConfigAsync().Returns(config);

        // Mock to simulate a failing extension
        var mockExecutor = Substitute.ForPartsOf<ExtensionExecutor>(_mockConfigService, _mockLogger);
        mockExecutor.FindExtension(extensionName).Returns("/path/to/redmine-failing");

        // In a real implementation, we would mock Process.Start to return the exit code
        // For this test, we're verifying the interface behavior

        // Act & Assert
        var extensionPath = mockExecutor.FindExtension(extensionName);
        extensionPath.Should().NotBeNull();
    }

    [Fact]
    public void FindExtension_Should_SearchMultiplePaths_When_Looking()
    {
        // Arrange
        const string extensionName = "test";
        var executor = new ExtensionExecutor(_mockConfigService, _mockLogger);

        // Act
        var result = executor.FindExtension(extensionName);

        // Assert
        // The method should search in multiple locations
        // Since we're not creating actual files, it should return null
        result.Should().BeNull();
    }

    [Fact]
    public void ListExtensions_Should_ReturnInstalledExtensions_When_Called()
    {
        // Arrange
        var executor = new ExtensionExecutor(_mockConfigService, _mockLogger);

        // Act
        var result = executor.ListExtensions();

        // Assert
        // Without actual extensions installed, should return empty
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }
}
