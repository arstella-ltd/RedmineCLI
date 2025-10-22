using System.Threading;
using System.Threading.Tasks;

using FluentAssertions;

using Microsoft.Extensions.Logging;

using NSubstitute;

using RedmineCLI.Commands;
using RedmineCLI.Models;
using RedmineCLI.Services;
using RedmineCLI.Tests.TestInfrastructure;

using Xunit;

namespace RedmineCLI.Tests.Commands;

[Collection("AnsiConsole")]
public class ConfigCommandTests
{
    private readonly IConfigService _configService;
    private readonly ILogger<ConfigCommand> _logger;
    private readonly ConfigCommand _command;
    private readonly AnsiConsoleTestFixture _consoleFixture;

    public ConfigCommandTests()
    {
        _configService = Substitute.For<IConfigService>();
        _logger = Substitute.For<ILogger<ConfigCommand>>();
        _command = new ConfigCommand(_configService, _logger);
        _consoleFixture = new AnsiConsoleTestFixture();
    }

    [Fact]
    public async Task SetAsync_Should_UpdateTimeFormat_WhenValidValue()
    {
        var config = CreateDefaultConfig();
        _configService.LoadConfigAsync().Returns(Task.FromResult(config));

        var (exitCode, output) = await _consoleFixture.ExecuteWithTestConsoleAsync(async console =>
        {
            var code = await _command.SetAsync("time.format", "absolute", CancellationToken.None);
            return (code, console.Output.ToString());
        });

        exitCode.Should().Be(0);
        config.Preferences.Time.Format.Should().Be("absolute");
        await _configService.Received(1).SaveConfigAsync(config);
        output.Should().Contain("Configuration updated");
    }

    [Fact]
    public async Task SetAsync_Should_ReturnError_WhenKeyUnknown()
    {
        var config = CreateDefaultConfig();
        _configService.LoadConfigAsync().Returns(Task.FromResult(config));

        var (exitCode, output) = await _consoleFixture.ExecuteWithTestConsoleAsync(async console =>
        {
            var code = await _command.SetAsync("unknown.key", "value", CancellationToken.None);
            return (code, console.Output.ToString());
        });

        exitCode.Should().Be(1);
        await _configService.DidNotReceive().SaveConfigAsync(Arg.Any<Config>());
        output.Should().Contain("Unknown configuration key");
    }

    [Fact]
    public async Task GetAsync_Should_WriteValue_WhenKeyExists()
    {
        var config = CreateDefaultConfig();
        config.Preferences.Time.Format = "utc";
        _configService.LoadConfigAsync().Returns(Task.FromResult(config));

        var (exitCode, output) = await _consoleFixture.ExecuteWithTestConsoleAsync(async console =>
        {
            var code = await _command.GetAsync("time.format", CancellationToken.None);
            return (code, console.Output.ToString());
        });

        exitCode.Should().Be(0);
        output.Should().Contain("utc");
    }

    [Fact]
    public async Task GetAsync_Should_ReturnError_WhenKeyUnknown()
    {
        var config = CreateDefaultConfig();
        _configService.LoadConfigAsync().Returns(Task.FromResult(config));

        var (exitCode, output) = await _consoleFixture.ExecuteWithTestConsoleAsync(async console =>
        {
            var code = await _command.GetAsync("missing.key", CancellationToken.None);
            return (code, console.Output.ToString());
        });

        exitCode.Should().Be(1);
        output.Should().Contain("Unknown configuration key");
    }

    [Fact]
    public async Task ListAsync_Should_DisplayConfigurationTable()
    {
        var config = CreateDefaultConfig();
        config.Preferences.Editor = "vim";
        config.Preferences.Time.Timezone = "Asia/Tokyo";
        _configService.LoadConfigAsync().Returns(Task.FromResult(config));

        var (exitCode, output) = await _consoleFixture.ExecuteWithTestConsoleAsync(async console =>
        {
            var code = await _command.ListAsync(CancellationToken.None);
            return (code, console.Output.ToString());
        });

        exitCode.Should().Be(0);
        output.Should().Contain("Configuration Settings");
        output.Should().Contain("time.format");
        output.Should().Contain("defaultformat");
        output.Should().Contain("Asia/Tokyo");
    }

    private static Config CreateDefaultConfig()
    {
        var config = new Config
        {
            CurrentProfile = "default",
            Preferences = new Preferences
            {
                DefaultFormat = "table",
                PageSize = 20,
                Editor = null,
                Time = new TimeSettings
                {
                    Format = "relative",
                    Timezone = "system"
                }
            }
        };

        config.Profiles["default"] = new Profile
        {
            Name = "default",
            Url = "https://example.com"
        };

        return config;
    }
}
