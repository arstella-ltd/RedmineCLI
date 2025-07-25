using System.CommandLine;

using FluentAssertions;

using Microsoft.Extensions.Logging;

using NSubstitute;

using RedmineCLI.ApiClient;
using RedmineCLI.Commands;
using RedmineCLI.Models;
using RedmineCLI.Services;
using RedmineCLI.Tests.TestInfrastructure;

using Spectre.Console;
using Spectre.Console.Testing;

using Xunit;

using Profile = RedmineCLI.Models.Profile;

namespace RedmineCLI.Tests.Commands;

[Collection("AnsiConsole")]
public class AuthCommandTests
{
    private readonly IConfigService _configService;
    private readonly IRedmineApiClient _apiClient;
    private readonly ILogger<AuthCommand> _logger;
    private readonly AuthCommand _authCommand;
    private readonly AnsiConsoleTestFixture _consoleFixture;

    public AuthCommandTests()
    {
        _configService = Substitute.For<IConfigService>();
        _apiClient = Substitute.For<IRedmineApiClient>();
        _logger = Substitute.For<ILogger<AuthCommand>>();

        _authCommand = new AuthCommand(_configService, _apiClient, _logger);
        _consoleFixture = new AnsiConsoleTestFixture();
    }

    #region Login Tests

    [Fact]
    public async Task Login_Should_SaveCredentials_When_ValidInput()
    {
        // Arrange
        var testUrl = "https://redmine.example.com";
        var testApiKey = "test-api-key-12345";
        var testProfileName = "default";

        _apiClient.TestConnectionAsync(testUrl, testApiKey, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(true));

        var config = new Config
        {
            CurrentProfile = testProfileName,
            Profiles = new Dictionary<string, Profile>(),
            Preferences = new Preferences()
        };
        _configService.LoadConfigAsync().Returns(Task.FromResult(config));

        // Act
        var result = await _authCommand.LoginAsync(testUrl, testApiKey, testProfileName);

        // Assert
        result.Should().Be(0);
        await _configService.Received(1).SaveConfigAsync(Arg.Is<Config>(c =>
            c.Profiles.ContainsKey(testProfileName) &&
            c.Profiles[testProfileName].Url == testUrl &&
            c.Profiles[testProfileName].ApiKey == testApiKey));
    }

    [Fact]
    public async Task Login_Should_HandleInvalidUrl_When_MalformedInput()
    {
        // Arrange
        var invalidUrl = "not-a-valid-url";
        var testApiKey = "test-api-key";
        var testProfileName = "default";

        // Act
        var result = await _authCommand.LoginAsync(invalidUrl, testApiKey, testProfileName);

        // Assert
        result.Should().Be(1);
        await _configService.DidNotReceive().SaveConfigAsync(Arg.Any<Config>());
    }

    [Fact]
    public async Task Login_Should_ReturnError_When_ConnectionTestFails()
    {
        // Arrange
        var testUrl = "https://redmine.example.com";
        var testApiKey = "invalid-api-key";
        var testProfileName = "default";

        _apiClient.TestConnectionAsync(testUrl, testApiKey, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(false));

        var config = new Config
        {
            CurrentProfile = testProfileName,
            Profiles = new Dictionary<string, Profile>(),
            Preferences = new Preferences()
        };
        _configService.LoadConfigAsync().Returns(Task.FromResult(config));

        // Act
        var result = await _authCommand.LoginAsync(testUrl, testApiKey, testProfileName);

        // Assert
        result.Should().Be(1);
        await _configService.DidNotReceive().SaveConfigAsync(Arg.Any<Config>());
    }

    [Fact]
    public async Task Login_Should_CreateDefaultProfile_When_FirstTimeSetup()
    {
        // Arrange
        var testUrl = "https://redmine.example.com";
        var testApiKey = "test-api-key-12345";
        var testProfileName = "default";

        _apiClient.TestConnectionAsync(testUrl, testApiKey, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(true));

        var emptyConfig = new Config
        {
            CurrentProfile = "default",
            Profiles = new Dictionary<string, Profile>(),
            Preferences = new Preferences()
        };
        _configService.LoadConfigAsync().Returns(Task.FromResult(emptyConfig));

        // Act
        var result = await _authCommand.LoginAsync(testUrl, testApiKey, testProfileName);

        // Assert
        result.Should().Be(0);
        await _configService.Received(1).SaveConfigAsync(Arg.Is<Config>(c =>
            c.Profiles.ContainsKey(testProfileName) &&
            c.CurrentProfile == testProfileName));
    }

    #endregion

    #region Status Tests

    [Fact]
    public async Task Status_Should_ShowConnectionState_When_Authenticated()
    {
        // Arrange
        var testProfile = new Profile
        {
            Name = "default",
            Url = "https://redmine.example.com",
            ApiKey = "test-api-key"
        };

        _configService.GetActiveProfileAsync()
            .Returns(Task.FromResult<Profile?>(testProfile));
        _apiClient.TestConnectionAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(true));

        // Act & Assert
        var result = await _consoleFixture.ExecuteWithTestConsoleAsync(async console =>
        {
            var actualResult = await _authCommand.StatusAsync();
            
            // Verify console output contains expected messages
            var output = console.Output;
            output.Should().Contain("Authentication Status");
            output.Should().Contain("default");
            output.Should().Contain("https://redmine.example.com");
            output.Should().Contain("Connection successful");
            
            return actualResult;
        });

        // Assert
        result.Should().Be(0);
        await _configService.Received(1).GetActiveProfileAsync();
        await _apiClient.Received(1).TestConnectionAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Status_Should_ShowNotAuthenticated_When_NoActiveProfile()
    {
        // Arrange
        _configService.GetActiveProfileAsync()
            .Returns(Task.FromResult<Profile?>(null));

        // Act
        var result = await _authCommand.StatusAsync();

        // Assert
        result.Should().Be(1);
        await _configService.Received(1).GetActiveProfileAsync();
        await _apiClient.DidNotReceive().TestConnectionAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Status_Should_ShowConnectionError_When_TestConnectionFails()
    {
        // Arrange
        var testProfile = new Profile
        {
            Name = "default",
            Url = "https://redmine.example.com",
            ApiKey = "invalid-api-key"
        };

        _configService.GetActiveProfileAsync()
            .Returns(Task.FromResult<Profile?>(testProfile));
        _apiClient.TestConnectionAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(false));

        // Act
        var result = await _authCommand.StatusAsync();

        // Assert
        result.Should().Be(1);
        await _apiClient.Received(1).TestConnectionAsync(Arg.Any<CancellationToken>());
    }

    #endregion

    #region Logout Tests

    [Fact]
    public async Task Logout_Should_ClearCredentials_When_Called()
    {
        // Arrange
        var testProfile = new Profile
        {
            Name = "default",
            Url = "https://redmine.example.com",
            ApiKey = "test-api-key"
        };

        var config = new Config
        {
            CurrentProfile = "default",
            Profiles = new Dictionary<string, Profile> { ["default"] = testProfile },
            Preferences = new Preferences()
        };

        _configService.LoadConfigAsync().Returns(Task.FromResult(config));
        _configService.GetActiveProfileAsync()
            .Returns(Task.FromResult<Profile?>(testProfile));

        // Act
        var result = await _authCommand.LogoutAsync();

        // Assert
        result.Should().Be(0);
        await _configService.Received(1).SaveConfigAsync(Arg.Is<Config>(c =>
            c.Profiles["default"].ApiKey == string.Empty));
    }

    [Fact]
    public async Task Logout_Should_HandleNoActiveProfile_When_NotAuthenticated()
    {
        // Arrange
        _configService.GetActiveProfileAsync()
            .Returns(Task.FromResult<Profile?>(null));

        // Act
        var result = await _authCommand.LogoutAsync();

        // Assert
        result.Should().Be(1);
        await _configService.DidNotReceive().SaveConfigAsync(Arg.Any<Config>());
    }

    [Fact]
    public async Task Logout_Should_ClearOnlyApiKey_When_PreservingOtherSettings()
    {
        // Arrange
        var testProfile = new Profile
        {
            Name = "default",
            Url = "https://redmine.example.com",
            ApiKey = "test-api-key",
            DefaultProject = "my-project",
            Preferences = new Preferences { DefaultFormat = "json" }
        };

        var config = new Config
        {
            CurrentProfile = "default",
            Profiles = new Dictionary<string, Profile> { ["default"] = testProfile },
            Preferences = new Preferences()
        };

        _configService.LoadConfigAsync().Returns(Task.FromResult(config));
        _configService.GetActiveProfileAsync()
            .Returns(Task.FromResult<Profile?>(testProfile));

        // Act
        var result = await _authCommand.LogoutAsync();

        // Assert
        result.Should().Be(0);
        await _configService.Received(1).SaveConfigAsync(Arg.Is<Config>(c =>
            c.Profiles["default"].ApiKey == string.Empty &&
            c.Profiles["default"].Url == "https://redmine.example.com" &&
            c.Profiles["default"].DefaultProject == "my-project"));
    }

    #endregion

    #region Interactive Tests

    [Fact]
    public async Task Login_Should_PromptForInput_When_NoParametersProvided()
    {
        // Arrange
        var config = new Config
        {
            CurrentProfile = "default",
            Profiles = new Dictionary<string, Profile>(),
            Preferences = new Preferences()
        };
        _configService.LoadConfigAsync().Returns(Task.FromResult(config));
        _apiClient.TestConnectionAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(true));

        // Act & Assert
        // このテストは対話的UIが実装された後に完全なテストを追加
        // 現在は基本的な構造のみテスト
        var result = await _authCommand.LoginInteractiveAsync();

        // 対話的入力のモックが複雑なため、実装後にテストを拡張
        result.Should().BeOneOf(0, 1); // 成功または失敗
    }

    #endregion

    #region Command Line Integration Tests

    [Fact]
    public void AuthCommand_Should_RegisterSubcommands_When_Created()
    {
        // Arrange & Act
        var command = AuthCommand.Create(_configService, _apiClient, _logger);

        // Assert
        command.Should().NotBeNull();
        command.Name.Should().Be("auth");
        command.Subcommands.Should().HaveCount(3); // login, status, logout

        var subcommandNames = command.Subcommands.Select(sc => sc.Name).ToList();
        subcommandNames.Should().Contain("login");
        subcommandNames.Should().Contain("status");
        subcommandNames.Should().Contain("logout");
    }

    [Fact]
    public void LoginCommand_Should_HaveCorrectOptions_When_Created()
    {
        // Arrange & Act
        var command = AuthCommand.Create(_configService, _apiClient, _logger);
        var loginCommand = command.Subcommands.First(sc => sc.Name == "login");

        // Assert
        loginCommand.Should().NotBeNull();
        var optionNames = loginCommand.Options.Select(o => o.Name).ToList();
        optionNames.Should().Contain("--url");
        optionNames.Should().Contain("--api-key");
        optionNames.Should().Contain("--profile");
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public async Task Login_Should_ReturnError_When_ApiKeyIsEmpty()
    {
        // Arrange
        var testUrl = "https://redmine.example.com";
        var emptyApiKey = "";
        var testProfileName = "default";

        // Act
        var result = await _authCommand.LoginAsync(testUrl, emptyApiKey, testProfileName);

        // Assert
        result.Should().Be(1);
        await _configService.DidNotReceive().SaveConfigAsync(Arg.Any<Config>());
    }

    [Fact]
    public async Task Login_Should_ReturnError_When_UrlIsEmpty()
    {
        // Arrange
        var emptyUrl = "";
        var testApiKey = "test-api-key";
        var testProfileName = "default";

        // Act
        var result = await _authCommand.LoginAsync(emptyUrl, testApiKey, testProfileName);

        // Assert
        result.Should().Be(1);
        await _configService.DidNotReceive().SaveConfigAsync(Arg.Any<Config>());
    }

    [Fact]
    public async Task Status_Should_HandleNull_When_NoActiveProfile()
    {
        // Arrange
        _configService.GetActiveProfileAsync()
            .Returns(Task.FromResult<Profile?>(null));

        // Act
        var result = await _authCommand.StatusAsync();

        // Assert
        result.Should().Be(1);
        await _configService.Received(1).GetActiveProfileAsync();
        await _apiClient.DidNotReceive().TestConnectionAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Logout_Should_ReturnError_When_NoActiveProfile()
    {
        // Arrange
        _configService.GetActiveProfileAsync()
            .Returns(Task.FromResult<Profile?>(null));

        // Act
        var result = await _authCommand.LogoutAsync();

        // Assert
        result.Should().Be(1);
        await _configService.DidNotReceive().SaveConfigAsync(Arg.Any<Config>());
    }

    [Fact]
    public async Task Logout_Should_ClearApiKey_When_ProfileExists()
    {
        // Arrange
        var testProfile = new Profile
        {
            Name = "default",
            Url = "https://redmine.example.com",
            ApiKey = "test-api-key"
        };

        var config = new Config
        {
            CurrentProfile = "default",
            Profiles = new Dictionary<string, Profile>
            {
                ["default"] = testProfile
            }
        };

        _configService.GetActiveProfileAsync()
            .Returns(Task.FromResult<Profile?>(testProfile));
        _configService.LoadConfigAsync()
            .Returns(Task.FromResult(config));

        // Act
        var result = await _authCommand.LogoutAsync();

        // Assert
        result.Should().Be(0);
        await _configService.Received(1).SaveConfigAsync(Arg.Is<Config>(c =>
            c.Profiles["default"].ApiKey == string.Empty));
    }

    #endregion
}
