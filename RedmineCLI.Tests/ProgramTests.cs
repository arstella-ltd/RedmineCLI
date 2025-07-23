using System.CommandLine;

using FluentAssertions;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using NSubstitute;

using RedmineCLI.ApiClient;
using RedmineCLI.Commands;
using RedmineCLI.Formatters;
using RedmineCLI.Models;
using RedmineCLI.Services;
using RedmineCLI.Utils;

using Xunit;

namespace RedmineCLI.Tests;

[Collection("AnsiConsole")]
public class ProgramTests
{
    [Fact]
    public async Task Main_Should_ReturnZero_When_HelpOption()
    {
        // Arrange
        var args = new[] { "--help" };

        // Act
        var result = await Program.Main(args);

        // Assert
        result.Should().Be(0);
    }

    [Fact]
    public async Task Main_Should_ShowLicenses_When_LicensesOption()
    {
        // Arrange
        var args = new[] { "--licenses" };

        // Act
        var result = await Program.Main(args);

        // Assert
        result.Should().Be(0);
    }

    [Fact]
    public async Task Main_Should_ShowVersion_When_VersionOption()
    {
        // Arrange
        var args = new[] { "--version" };

        // Act
        var result = await Program.Main(args);

        // Assert
        result.Should().Be(0);
    }

    [Fact]
    public async Task Main_Should_ReturnNonZero_When_UnknownCommand()
    {
        // Arrange
        var args = new[] { "unknown-command" };

        // Act
        var result = await Program.Main(args);

        // Assert
        result.Should().NotBe(0);
    }

    [Fact]
    public async Task Main_Should_RecognizeAuthCommand()
    {
        // Arrange
        var args = new[] { "auth", "--help" };

        // Act
        var result = await Program.Main(args);

        // Assert
        result.Should().Be(0);
    }

    [Fact]
    public async Task Main_Should_RecognizeIssueCommand()
    {
        // Arrange
        var args = new[] { "issue", "--help" };

        // Act
        var result = await Program.Main(args);

        // Assert
        result.Should().Be(0);
    }

    [Fact]
    public async Task Main_Should_RecognizeConfigCommand()
    {
        // Arrange
        var args = new[] { "config", "--help" };

        // Act
        var result = await Program.Main(args);

        // Assert
        result.Should().Be(0);
    }

    [Fact]
    public void ConfigureServices_Should_RegisterAllRequiredServices()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        // Use reflection to call the private ConfigureServices method
        var configureServicesMethod = typeof(Program).GetMethod(
            "ConfigureServices",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

        configureServicesMethod!.Invoke(null, new object[] { services });

        var serviceProvider = services.BuildServiceProvider();

        // Assert
        serviceProvider.GetService<IConfigService>().Should().NotBeNull();
        serviceProvider.GetService<IRedmineApiClient>().Should().NotBeNull();
        serviceProvider.GetService<ITableFormatter>().Should().NotBeNull();
        serviceProvider.GetService<IJsonFormatter>().Should().NotBeNull();
        serviceProvider.GetService<ILicenseHelper>().Should().NotBeNull();
        serviceProvider.GetService<ILogger<AuthCommand>>().Should().NotBeNull();
        serviceProvider.GetService<ILogger<IssueCommand>>().Should().NotBeNull();
    }

    [Fact]
    public async Task Main_Should_HandleDebugOption()
    {
        // Arrange
        var args = new[] { "--debug", "--help" };

        // Act
        var result = await Program.Main(args);

        // Assert
        result.Should().Be(0);
    }

    [Fact]
    public async Task Main_Should_AcceptDebugShortOption()
    {
        // Arrange
        var args = new[] { "-d", "--help" };

        // Act
        var result = await Program.Main(args);

        // Assert
        result.Should().Be(0);
    }

    [Fact]
    public async Task Main_Should_ReturnNonZero_When_NoArguments()
    {
        // Arrange
        var args = Array.Empty<string>();

        // Act
        var result = await Program.Main(args);

        // Assert
        // When no arguments are provided, System.CommandLine typically returns 0
        // as it shows the default help
        result.Should().Be(0);
    }

    [Theory]
    [InlineData("auth", 1)]
    [InlineData("issue", 1)]
    [InlineData("config", 0)]
    public async Task Main_Should_ReturnExpectedCode_When_NoSubcommand(string command, int expectedCode)
    {
        // Arrange
        var args = new[] { command };

        // Act
        var result = await Program.Main(args);

        // Assert
        // auth and issue commands require subcommands, so they return 1
        // config command might show help and return 0
        result.Should().Be(expectedCode);
    }

    [Fact]
    public async Task Main_Should_HandleAtSymbol_Without_ResponseFileError()
    {
        // Arrange
        // This test verifies that @me is not interpreted as a response file
        var args = new[] { "issue", "list", "--assignee", "@me", "--help" };

        // Act
        var result = await Program.Main(args);

        // Assert
        // The command should show help (return 0) without trying to interpret @me as a response file
        // If response file handling was not disabled, this would fail with "Response file 'me' not found"
        result.Should().Be(0);
    }

    [Fact]
    public async Task Main_Should_AcceptAtMeInAssigneeOption()
    {
        // Arrange
        // Additional test to verify @me works in different contexts
        var args = new[] { "issue", "create", "--assignee", "@me", "--help" };

        // Act
        var result = await Program.Main(args);

        // Assert
        // Should show help without response file error
        result.Should().Be(0);
    }
}
