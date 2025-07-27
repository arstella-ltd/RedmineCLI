using System.CommandLine;

using FluentAssertions;

using Microsoft.Extensions.Logging;

using NSubstitute;
using NSubstitute.ExceptionExtensions;

using RedmineCLI.Commands;
using RedmineCLI.Exceptions;
using RedmineCLI.Formatters;
using RedmineCLI.Models;
using RedmineCLI.Services;

using Xunit;

namespace RedmineCLI.Tests.Commands;

public class PriorityCommandTests
{
    private readonly IRedmineService _redmineService;
    private readonly IConfigService _configService;
    private readonly ITableFormatter _tableFormatter;
    private readonly IJsonFormatter _jsonFormatter;
    private readonly ILogger<PriorityCommand> _logger;

    public PriorityCommandTests()
    {
        _redmineService = Substitute.For<IRedmineService>();
        _configService = Substitute.For<IConfigService>();
        _tableFormatter = Substitute.For<ITableFormatter>();
        _jsonFormatter = Substitute.For<IJsonFormatter>();
        _logger = Substitute.For<ILogger<PriorityCommand>>();

        // Setup default config to avoid null reference
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
        var command = PriorityCommand.Create(_redmineService, _configService, _tableFormatter, _jsonFormatter, _logger);
        var listCommand = command.Subcommands.First(c => c.Name == "list");

        // Assert
        listCommand.Aliases.Should().Contain("ls");
    }

    [Fact]
    public async Task List_Should_ReturnAllPriorities_When_Called()
    {
        // Arrange
        var priorities = new List<Priority>
        {
            new Priority { Id = 1, Name = "Low" },
            new Priority { Id = 2, Name = "Normal" },
            new Priority { Id = 3, Name = "High" },
            new Priority { Id = 4, Name = "Urgent" },
            new Priority { Id = 5, Name = "Immediate" }
        };

        _redmineService.GetPrioritiesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(priorities));

        var command = PriorityCommand.Create(_redmineService, _configService, _tableFormatter, _jsonFormatter, _logger);
        var parseResult = command.Parse("list");

        // Act
        var result = await parseResult.InvokeAsync();

        // Assert
        result.Should().Be(0);
        await _redmineService.Received(1).GetPrioritiesAsync(Arg.Any<CancellationToken>());
        _tableFormatter.Received(1).FormatPriorities(priorities);
    }

    [Fact]
    public async Task List_Should_FormatAsJson_When_JsonOptionIsSet()
    {
        // Arrange
        var priorities = new List<Priority>
        {
            new Priority { Id = 1, Name = "Low" },
            new Priority { Id = 2, Name = "Normal" },
            new Priority { Id = 3, Name = "High" }
        };

        _redmineService.GetPrioritiesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(priorities));

        var command = PriorityCommand.Create(_redmineService, _configService, _tableFormatter, _jsonFormatter, _logger);
        var parseResult = command.Parse("list --json");

        // Act
        var result = await parseResult.InvokeAsync();

        // Assert
        result.Should().Be(0);
        await _redmineService.Received(1).GetPrioritiesAsync(Arg.Any<CancellationToken>());
        _jsonFormatter.Received(1).FormatPriorities(priorities);
        _tableFormatter.DidNotReceive().FormatPriorities(Arg.Any<List<Priority>>());
    }

    [Fact]
    public async Task List_Should_ShowIdAndName_When_Called()
    {
        // Arrange
        var priorities = new List<Priority>
        {
            new Priority { Id = 2, Name = "Normal" },
            new Priority { Id = 3, Name = "High" }
        };

        _redmineService.GetPrioritiesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(priorities));

        var command = PriorityCommand.Create(_redmineService, _configService, _tableFormatter, _jsonFormatter, _logger);
        var parseResult = command.Parse("list");

        // Act
        var result = await parseResult.InvokeAsync();

        // Assert
        result.Should().Be(0);
        _tableFormatter.Received(1).FormatPriorities(
            Arg.Is<List<Priority>>(list =>
                list.Count == 2 &&
                list.Any(p => p.Id == 2 && p.Name == "Normal") &&
                list.Any(p => p.Id == 3 && p.Name == "High")));
    }

    [Fact]
    public async Task List_Should_SetAbsoluteTimeFormat_When_ConfiguredAsAbsolute()
    {
        // Arrange
        var config = new Config
        {
            CurrentProfile = "default",
            Profiles = new Dictionary<string, Profile>(),
            Preferences = new Preferences
            {
                Time = new TimeSettings { Format = "absolute" }
            }
        };
        _configService.LoadConfigAsync().Returns(Task.FromResult(config));

        var priorities = new List<Priority>
        {
            new Priority { Id = 1, Name = "Low" }
        };
        _redmineService.GetPrioritiesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(priorities));

        var command = PriorityCommand.Create(_redmineService, _configService, _tableFormatter, _jsonFormatter, _logger);
        var parseResult = command.Parse("list");

        // Act
        var result = await parseResult.InvokeAsync();

        // Assert
        result.Should().Be(0);
        _tableFormatter.Received(1).SetTimeFormat(TimeFormat.Absolute);
    }

    [Fact]
    public async Task List_Should_SetUtcTimeFormat_When_ConfiguredAsUtc()
    {
        // Arrange
        var config = new Config
        {
            CurrentProfile = "default",
            Profiles = new Dictionary<string, Profile>(),
            Preferences = new Preferences
            {
                Time = new TimeSettings { Format = "utc" }
            }
        };
        _configService.LoadConfigAsync().Returns(Task.FromResult(config));

        var priorities = new List<Priority>
        {
            new Priority { Id = 1, Name = "Low" }
        };
        _redmineService.GetPrioritiesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(priorities));

        var command = PriorityCommand.Create(_redmineService, _configService, _tableFormatter, _jsonFormatter, _logger);
        var parseResult = command.Parse("list");

        // Act
        var result = await parseResult.InvokeAsync();

        // Assert
        result.Should().Be(0);
        _tableFormatter.Received(1).SetTimeFormat(TimeFormat.Utc);
    }

    [Fact]
    public async Task List_Should_HandleApiException_When_ForbiddenError()
    {
        // Arrange
        var apiException = new RedmineApiException(403, "Forbidden");
        _redmineService.GetPrioritiesAsync(Arg.Any<CancellationToken>())
            .ThrowsAsync(apiException);

        var command = PriorityCommand.Create(_redmineService, _configService, _tableFormatter, _jsonFormatter, _logger);
        var parseResult = command.Parse("list");

        // Act
        Environment.ExitCode = 0; // Reset exit code
        await parseResult.InvokeAsync();

        // Assert
        Environment.ExitCode.Should().Be(1);
        _logger.Received(1).LogError(apiException, "API error while listing priorities");
    }

    [Fact]
    public async Task List_Should_HandleApiException_When_OtherApiError()
    {
        // Arrange
        var apiException = new RedmineApiException(400, "Bad Request");
        _redmineService.GetPrioritiesAsync(Arg.Any<CancellationToken>())
            .ThrowsAsync(apiException);

        var command = PriorityCommand.Create(_redmineService, _configService, _tableFormatter, _jsonFormatter, _logger);
        var parseResult = command.Parse("list");

        // Act
        Environment.ExitCode = 0; // Reset exit code
        await parseResult.InvokeAsync();

        // Assert
        Environment.ExitCode.Should().Be(1);
        _logger.Received(1).LogError(apiException, "API error while listing priorities");
    }

    [Fact]
    public async Task List_Should_HandleGeneralException_When_UnexpectedError()
    {
        // Arrange
        var exception = new InvalidOperationException("Something went wrong");
        _redmineService.GetPrioritiesAsync(Arg.Any<CancellationToken>())
            .ThrowsAsync(exception);

        var command = PriorityCommand.Create(_redmineService, _configService, _tableFormatter, _jsonFormatter, _logger);
        var parseResult = command.Parse("list");

        // Act
        Environment.ExitCode = 0; // Reset exit code
        await parseResult.InvokeAsync();

        // Assert
        Environment.ExitCode.Should().Be(1);
        _logger.Received(1).LogError(exception, "Unexpected error while listing priorities");
    }
}
