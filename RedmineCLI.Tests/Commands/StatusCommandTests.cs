using System.CommandLine;

using FluentAssertions;

using Microsoft.Extensions.Logging;

using NSubstitute;

using RedmineCLI.ApiClient;
using RedmineCLI.Commands;
using RedmineCLI.Formatters;
using RedmineCLI.Models;
using RedmineCLI.Services;

using Xunit;

namespace RedmineCLI.Tests.Commands;

public class StatusCommandTests
{
    private readonly IRedmineApiClient _apiClient;
    private readonly IConfigService _configService;
    private readonly ITableFormatter _tableFormatter;
    private readonly IJsonFormatter _jsonFormatter;
    private readonly ILogger<StatusCommand> _logger;

    public StatusCommandTests()
    {
        _apiClient = Substitute.For<IRedmineApiClient>();
        _configService = Substitute.For<IConfigService>();
        _tableFormatter = Substitute.For<ITableFormatter>();
        _jsonFormatter = Substitute.For<IJsonFormatter>();
        _logger = Substitute.For<ILogger<StatusCommand>>();

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
        var command = StatusCommand.Create(_apiClient, _configService, _tableFormatter, _jsonFormatter, _logger);
        var listCommand = command.Subcommands.First(c => c.Name == "list");

        // Assert
        listCommand.Aliases.Should().Contain("ls");
    }

    [Fact]
    public async Task List_Should_ReturnAllStatuses_When_Called()
    {
        // Arrange
        var statuses = new List<IssueStatus>
        {
            new IssueStatus
            {
                Id = 1,
                Name = "New",
                IsClosed = false,
                IsDefault = true
            },
            new IssueStatus
            {
                Id = 2,
                Name = "In Progress",
                IsClosed = false,
                IsDefault = false
            },
            new IssueStatus
            {
                Id = 3,
                Name = "Resolved",
                IsClosed = false,
                IsDefault = false
            },
            new IssueStatus
            {
                Id = 4,
                Name = "Closed",
                IsClosed = true,
                IsDefault = false
            }
        };

        _apiClient.GetIssueStatusesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(statuses));

        var command = StatusCommand.Create(_apiClient, _configService, _tableFormatter, _jsonFormatter, _logger);
        var parseResult = command.Parse("list");

        // Act
        var result = await parseResult.InvokeAsync();

        // Assert
        result.Should().Be(0);
        await _apiClient.Received(1).GetIssueStatusesAsync(Arg.Any<CancellationToken>());
        _tableFormatter.Received(1).FormatIssueStatuses(statuses);
    }

    [Fact]
    public async Task List_Should_ShowClosedFlag_When_StatusIsClosed()
    {
        // Arrange
        var statuses = new List<IssueStatus>
        {
            new IssueStatus
            {
                Id = 4,
                Name = "Closed",
                IsClosed = true,
                IsDefault = false
            }
        };

        _apiClient.GetIssueStatusesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(statuses));

        var command = StatusCommand.Create(_apiClient, _configService, _tableFormatter, _jsonFormatter, _logger);
        var parseResult = command.Parse("list");

        // Act
        var result = await parseResult.InvokeAsync();

        // Assert
        result.Should().Be(0);
        _tableFormatter.Received(1).FormatIssueStatuses(
            Arg.Is<List<IssueStatus>>(list =>
                list.Any(s => s.IsClosed == true && s.Name == "Closed")));
    }

    [Fact]
    public async Task List_Should_ShowDefaultFlag_When_StatusIsDefault()
    {
        // Arrange
        var statuses = new List<IssueStatus>
        {
            new IssueStatus
            {
                Id = 1,
                Name = "New",
                IsClosed = false,
                IsDefault = true
            }
        };

        _apiClient.GetIssueStatusesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(statuses));

        var command = StatusCommand.Create(_apiClient, _configService, _tableFormatter, _jsonFormatter, _logger);
        var parseResult = command.Parse("list");

        // Act
        var result = await parseResult.InvokeAsync();

        // Assert
        result.Should().Be(0);
        _tableFormatter.Received(1).FormatIssueStatuses(
            Arg.Is<List<IssueStatus>>(list =>
                list.Any(s => s.IsDefault == true && s.Name == "New")));
    }

    [Fact]
    public async Task List_Should_FormatAsJson_When_JsonOptionIsSet()
    {
        // Arrange
        var statuses = new List<IssueStatus>
        {
            new IssueStatus
            {
                Id = 1,
                Name = "New",
                IsClosed = false,
                IsDefault = true
            }
        };

        _apiClient.GetIssueStatusesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(statuses));

        var command = StatusCommand.Create(_apiClient, _configService, _tableFormatter, _jsonFormatter, _logger);
        var parseResult = command.Parse("list --json");

        // Act
        var result = await parseResult.InvokeAsync();

        // Assert
        result.Should().Be(0);
        await _apiClient.Received(1).GetIssueStatusesAsync(Arg.Any<CancellationToken>());
        _jsonFormatter.Received(1).FormatIssueStatuses(statuses);
        _tableFormatter.DidNotReceive().FormatIssueStatuses(Arg.Any<List<IssueStatus>>());
    }
}
