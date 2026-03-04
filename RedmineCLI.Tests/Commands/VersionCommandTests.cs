using System.CommandLine;

using FluentAssertions;

using Microsoft.Extensions.Logging;

using NSubstitute;

using RedmineCLI.Commands;
using RedmineCLI.Formatters;
using RedmineCLI.Models;
using RedmineCLI.Services;

using Xunit;

namespace RedmineCLI.Tests.Commands;

public class VersionCommandTests
{
    private readonly IRedmineService _redmineService;
    private readonly IConfigService _configService;
    private readonly ITableFormatter _tableFormatter;
    private readonly IJsonFormatter _jsonFormatter;
    private readonly ILogger<VersionCommand> _logger;

    public VersionCommandTests()
    {
        _redmineService = Substitute.For<IRedmineService>();
        _configService = Substitute.For<IConfigService>();
        _tableFormatter = Substitute.For<ITableFormatter>();
        _jsonFormatter = Substitute.For<IJsonFormatter>();
        _logger = Substitute.For<ILogger<VersionCommand>>();

        var config = new Config
        {
            CurrentProfile = "default",
            Profiles = new Dictionary<string, Profile>(),
            Preferences = new Preferences()
        };
        _configService.LoadConfigAsync().Returns(Task.FromResult(config));
    }

    [Fact]
    public void Command_Should_HaveLsAlias()
    {
        // Arrange & Act
        var command = VersionCommand.Create(_redmineService, _configService, _tableFormatter, _jsonFormatter, _logger);
        var listCommand = command.Subcommands.First(c => c.Name == "list");

        // Assert
        listCommand.Aliases.Should().Contain("ls");
    }

    [Fact]
    public async Task List_Should_ReturnVersions_When_ProjectIsSpecified()
    {
        // Arrange
        var versions = new List<TargetVersion>
        {
            new TargetVersion { Id = 1, Name = "v1.0.0", Status = "closed" },
            new TargetVersion { Id = 2, Name = "v1.1.0", Status = "open" },
            new TargetVersion { Id = 3, Name = "v2.0.0", Status = "locked" }
        };

        _redmineService.GetVersionsAsync("my-project", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(versions));

        var command = VersionCommand.Create(_redmineService, _configService, _tableFormatter, _jsonFormatter, _logger);
        var parseResult = command.Parse("list --project my-project");

        // Act
        var result = await parseResult.InvokeAsync();

        // Assert
        result.Should().Be(0);
        await _redmineService.Received(1).GetVersionsAsync("my-project", Arg.Any<CancellationToken>());
        _tableFormatter.Received(1).FormatVersions(versions);
    }

    [Fact]
    public async Task List_Should_FormatAsJson_When_JsonOptionIsSet()
    {
        // Arrange
        var versions = new List<TargetVersion>
        {
            new TargetVersion { Id = 1, Name = "v1.0.0", Status = "open" }
        };

        _redmineService.GetVersionsAsync("my-project", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(versions));

        var command = VersionCommand.Create(_redmineService, _configService, _tableFormatter, _jsonFormatter, _logger);
        var parseResult = command.Parse("list --project my-project --json");

        // Act
        var result = await parseResult.InvokeAsync();

        // Assert
        result.Should().Be(0);
        await _redmineService.Received(1).GetVersionsAsync("my-project", Arg.Any<CancellationToken>());
        _jsonFormatter.Received(1).FormatVersions(versions);
        _tableFormatter.DidNotReceive().FormatVersions(Arg.Any<List<TargetVersion>>());
    }

    [Fact]
    public async Task List_Should_ReturnError_When_ProjectIsNotSpecified()
    {
        // Arrange
        var command = VersionCommand.Create(_redmineService, _configService, _tableFormatter, _jsonFormatter, _logger);
        var parseResult = command.Parse("list");

        // Act
        var result = await parseResult.InvokeAsync();

        // Assert
        // System.CommandLine returns non-zero for missing required options
        result.Should().NotBe(0);
        await _redmineService.DidNotReceive().GetVersionsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }
}
