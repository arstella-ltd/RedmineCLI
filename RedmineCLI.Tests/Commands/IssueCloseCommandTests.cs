using System.CommandLine;

using FluentAssertions;

using Microsoft.Extensions.Logging;

using NSubstitute;

using RedmineCLI.Commands;
using RedmineCLI.Exceptions;
using RedmineCLI.Formatters;
using RedmineCLI.Models;
using RedmineCLI.Services;

using Xunit;

namespace RedmineCLI.Tests.Commands;

public class IssueCloseCommandTests
{
    private readonly IRedmineService _redmineService;
    private readonly IConfigService _configService;
    private readonly ITableFormatter _tableFormatter;
    private readonly IJsonFormatter _jsonFormatter;
    private readonly ILogger<IssueCommand> _logger;
    private readonly IssueCommand _issueCommand;

    public IssueCloseCommandTests()
    {
        _redmineService = Substitute.For<IRedmineService>();
        _configService = Substitute.For<IConfigService>();
        _tableFormatter = Substitute.For<ITableFormatter>();
        _jsonFormatter = Substitute.For<IJsonFormatter>();
        _logger = Substitute.For<ILogger<IssueCommand>>();

        // Setup default config to avoid null reference
        var config = new Config
        {
            CurrentProfile = "default",
            Profiles = new Dictionary<string, Profile>(),
            Preferences = new Preferences()
        };
        _configService.LoadConfigAsync().Returns(Task.FromResult(config));

        _issueCommand = new IssueCommand(_redmineService, _configService, _tableFormatter, _jsonFormatter, _logger);
    }

    [Fact]
    public async Task Close_Should_UseDefaultCloseStatus_When_StatusNotSpecified()
    {
        // Arrange
        var issueIds = new[] { 123 };
        var statuses = new List<IssueStatus>
        {
            new IssueStatus { Id = 1, Name = "New", IsClosed = false },
            new IssueStatus { Id = 5, Name = "Closed", IsClosed = true },
            new IssueStatus { Id = 6, Name = "Rejected", IsClosed = true }
        };
        var currentIssue = new Issue
        {
            Id = 123,
            Subject = "Test Issue",
            Status = new IssueStatus { Id = 1, Name = "New", IsClosed = false }
        };
        var updatedIssue = new Issue
        {
            Id = 123,
            Subject = "Test Issue",
            Status = new IssueStatus { Id = 5, Name = "Closed", IsClosed = true },
            DoneRatio = 100
        };

        _redmineService.GetIssueStatusesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(statuses));
        _redmineService.GetIssueAsync(123, false, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(currentIssue));
        _redmineService.UpdateIssueAsync(123, null, "5", null, 100, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(updatedIssue));

        // Act
        var result = await _issueCommand.CloseAsync(issueIds, null, null, null, false, CancellationToken.None);

        // Assert
        result.Should().Be(0);
        await _redmineService.Received(1).UpdateIssueAsync(123, null, "5", null, 100, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Close_Should_UseSpecifiedStatus_When_StatusProvided()
    {
        // Arrange
        var issueIds = new[] { 123 };
        var statuses = new List<IssueStatus>
        {
            new IssueStatus { Id = 1, Name = "New", IsClosed = false },
            new IssueStatus { Id = 5, Name = "Closed", IsClosed = true },
            new IssueStatus { Id = 6, Name = "Rejected", IsClosed = true }
        };
        var currentIssue = new Issue
        {
            Id = 123,
            Subject = "Test Issue",
            Status = new IssueStatus { Id = 1, Name = "New", IsClosed = false }
        };
        var updatedIssue = new Issue
        {
            Id = 123,
            Subject = "Test Issue",
            Status = new IssueStatus { Id = 6, Name = "Rejected", IsClosed = true },
            DoneRatio = 100
        };

        _redmineService.GetIssueStatusesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(statuses));
        _redmineService.GetIssueAsync(123, false, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(currentIssue));
        _redmineService.UpdateIssueAsync(123, null, "6", null, 100, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(updatedIssue));

        // Act
        var result = await _issueCommand.CloseAsync(issueIds, null, "6", null, false, CancellationToken.None);

        // Assert
        result.Should().Be(0);
        await _redmineService.Received(1).UpdateIssueAsync(123, null, "6", null, 100, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Close_Should_AddComment_When_MessageProvided()
    {
        // Arrange
        var issueIds = new[] { 123 };
        var message = "Closing this issue";
        var statuses = new List<IssueStatus>
        {
            new IssueStatus { Id = 5, Name = "Closed", IsClosed = true }
        };
        var currentIssue = new Issue
        {
            Id = 123,
            Subject = "Test Issue",
            Status = new IssueStatus { Id = 1, Name = "New", IsClosed = false }
        };
        var updatedIssue = new Issue
        {
            Id = 123,
            Subject = "Test Issue",
            Status = new IssueStatus { Id = 5, Name = "Closed", IsClosed = true },
            DoneRatio = 100
        };

        _redmineService.GetIssueStatusesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(statuses));
        _redmineService.GetIssueAsync(123, false, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(currentIssue));
        _redmineService.UpdateIssueAsync(123, null, "5", null, 100, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(updatedIssue));

        // Act
        var result = await _issueCommand.CloseAsync(issueIds, message, null, null, false, CancellationToken.None);

        // Assert
        result.Should().Be(0);
        await _redmineService.Received(1).AddCommentAsync(123, message, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Close_Should_SetDoneRatio_When_Specified()
    {
        // Arrange
        var issueIds = new[] { 123 };
        var doneRatio = 80;
        var statuses = new List<IssueStatus>
        {
            new IssueStatus { Id = 5, Name = "Closed", IsClosed = true }
        };
        var currentIssue = new Issue
        {
            Id = 123,
            Subject = "Test Issue",
            Status = new IssueStatus { Id = 1, Name = "New", IsClosed = false }
        };
        var updatedIssue = new Issue
        {
            Id = 123,
            Subject = "Test Issue",
            Status = new IssueStatus { Id = 5, Name = "Closed", IsClosed = true },
            DoneRatio = doneRatio
        };

        _redmineService.GetIssueStatusesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(statuses));
        _redmineService.GetIssueAsync(123, false, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(currentIssue));
        _redmineService.UpdateIssueAsync(123, null, "5", null, doneRatio, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(updatedIssue));

        // Act
        var result = await _issueCommand.CloseAsync(issueIds, null, null, doneRatio, false, CancellationToken.None);

        // Assert
        result.Should().Be(0);
        await _redmineService.Received(1).UpdateIssueAsync(123, null, "5", null, doneRatio, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Close_Should_Default100DoneRatio_When_NotSpecified()
    {
        // Arrange
        var issueIds = new[] { 123 };
        var statuses = new List<IssueStatus>
        {
            new IssueStatus { Id = 5, Name = "Closed", IsClosed = true }
        };
        var currentIssue = new Issue
        {
            Id = 123,
            Subject = "Test Issue",
            Status = new IssueStatus { Id = 1, Name = "New", IsClosed = false }
        };
        var updatedIssue = new Issue
        {
            Id = 123,
            Subject = "Test Issue",
            Status = new IssueStatus { Id = 5, Name = "Closed", IsClosed = true },
            DoneRatio = 100
        };

        _redmineService.GetIssueStatusesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(statuses));
        _redmineService.GetIssueAsync(123, false, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(currentIssue));
        _redmineService.UpdateIssueAsync(123, null, "5", null, 100, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(updatedIssue));

        // Act
        var result = await _issueCommand.CloseAsync(issueIds, null, null, null, false, CancellationToken.None);

        // Assert
        result.Should().Be(0);
        await _redmineService.Received(1).UpdateIssueAsync(123, null, "5", null, 100, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Close_Should_SkipAlreadyClosedIssues_With_Warning()
    {
        // Arrange
        var issueIds = new[] { 123 };
        var statuses = new List<IssueStatus>
        {
            new IssueStatus { Id = 5, Name = "Closed", IsClosed = true }
        };
        var currentIssue = new Issue
        {
            Id = 123,
            Subject = "Test Issue",
            Status = new IssueStatus { Id = 5, Name = "Closed", IsClosed = true }
        };

        _redmineService.GetIssueStatusesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(statuses));
        _redmineService.GetIssueAsync(123, false, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(currentIssue));

        // Act
        var result = await _issueCommand.CloseAsync(issueIds, null, null, null, false, CancellationToken.None);

        // Assert
        result.Should().Be(0);
        await _redmineService.DidNotReceive().UpdateIssueAsync(Arg.Any<int>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<int?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Close_Should_WarnWhenStatusIsNotClosed_But_Proceed()
    {
        // Arrange
        var issueIds = new[] { 123 };
        var statuses = new List<IssueStatus>
        {
            new IssueStatus { Id = 2, Name = "In Progress", IsClosed = false },
            new IssueStatus { Id = 5, Name = "Closed", IsClosed = true }
        };
        var currentIssue = new Issue
        {
            Id = 123,
            Subject = "Test Issue",
            Status = new IssueStatus { Id = 1, Name = "New", IsClosed = false }
        };
        var updatedIssue = new Issue
        {
            Id = 123,
            Subject = "Test Issue",
            Status = new IssueStatus { Id = 2, Name = "In Progress", IsClosed = false },
            DoneRatio = 100
        };

        _redmineService.GetIssueStatusesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(statuses));
        _redmineService.GetIssueAsync(123, false, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(currentIssue));
        _redmineService.UpdateIssueAsync(123, null, "2", null, 100, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(updatedIssue));

        // Act
        var result = await _issueCommand.CloseAsync(issueIds, null, "2", null, false, CancellationToken.None);

        // Assert
        result.Should().Be(0);
        await _redmineService.Received(1).UpdateIssueAsync(123, null, "2", null, 100, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Close_Should_HandleMultipleIssues()
    {
        // Arrange
        var issueIds = new[] { 123, 124, 125 };
        var statuses = new List<IssueStatus>
        {
            new IssueStatus { Id = 5, Name = "Closed", IsClosed = true }
        };
        var issues = new[]
        {
            new Issue { Id = 123, Subject = "Issue 1", Status = new IssueStatus { Id = 1, Name = "New", IsClosed = false } },
            new Issue { Id = 124, Subject = "Issue 2", Status = new IssueStatus { Id = 1, Name = "New", IsClosed = false } },
            new Issue { Id = 125, Subject = "Issue 3", Status = new IssueStatus { Id = 1, Name = "New", IsClosed = false } }
        };
        var updatedIssues = new[]
        {
            new Issue { Id = 123, Subject = "Issue 1", Status = new IssueStatus { Id = 5, Name = "Closed", IsClosed = true }, DoneRatio = 100 },
            new Issue { Id = 124, Subject = "Issue 2", Status = new IssueStatus { Id = 5, Name = "Closed", IsClosed = true }, DoneRatio = 100 },
            new Issue { Id = 125, Subject = "Issue 3", Status = new IssueStatus { Id = 5, Name = "Closed", IsClosed = true }, DoneRatio = 100 }
        };

        _redmineService.GetIssueStatusesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(statuses));

        for (int i = 0; i < issueIds.Length; i++)
        {
            var id = issueIds[i];
            var issue = issues[i];
            var updatedIssue = updatedIssues[i];

            _redmineService.GetIssueAsync(id, false, Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(issue));
            _redmineService.UpdateIssueAsync(id, null, "5", null, 100, Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(updatedIssue));
        }

        // Act
        var result = await _issueCommand.CloseAsync(issueIds, null, null, null, false, CancellationToken.None);

        // Assert
        result.Should().Be(0);
        await _redmineService.Received(3).UpdateIssueAsync(Arg.Any<int>(), null, "5", null, 100, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Close_Should_ReturnError_When_NoClosedStatusExists()
    {
        // Arrange
        var issueIds = new[] { 123 };
        var statuses = new List<IssueStatus>
        {
            new IssueStatus { Id = 1, Name = "New", IsClosed = false },
            new IssueStatus { Id = 2, Name = "In Progress", IsClosed = false }
        };

        _redmineService.GetIssueStatusesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(statuses));

        // Act
        var result = await _issueCommand.CloseAsync(issueIds, null, null, null, false, CancellationToken.None);

        // Assert
        result.Should().Be(1);
        await _redmineService.DidNotReceive().UpdateIssueAsync(Arg.Any<int>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<int?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Close_Should_ReturnError_When_IssueNotFound()
    {
        // Arrange
        var issueIds = new[] { 123, 999 };
        var statuses = new List<IssueStatus>
        {
            new IssueStatus { Id = 5, Name = "Closed", IsClosed = true }
        };
        var issue123 = new Issue
        {
            Id = 123,
            Subject = "Test Issue",
            Status = new IssueStatus { Id = 1, Name = "New", IsClosed = false }
        };
        var updatedIssue123 = new Issue
        {
            Id = 123,
            Subject = "Test Issue",
            Status = new IssueStatus { Id = 5, Name = "Closed", IsClosed = true },
            DoneRatio = 100
        };

        _redmineService.GetIssueStatusesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(statuses));
        _redmineService.GetIssueAsync(123, false, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(issue123));
        _redmineService.GetIssueAsync(999, false, Arg.Any<CancellationToken>())
            .Returns<Issue>(x => throw new RedmineApiException(404, "Not found"));
        _redmineService.UpdateIssueAsync(123, null, "5", null, 100, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(updatedIssue123));

        // Act
        var result = await _issueCommand.CloseAsync(issueIds, null, null, null, false, CancellationToken.None);

        // Assert
        result.Should().Be(1); // Error due to issue 999 not found
        await _redmineService.Received(1).UpdateIssueAsync(123, null, "5", null, 100, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Close_Should_ReturnError_When_DoneRatioInvalid()
    {
        // Arrange
        var issueIds = new[] { 123 };

        // Act
        var result = await _issueCommand.CloseAsync(issueIds, null, null, 150, false, CancellationToken.None);

        // Assert
        result.Should().Be(1);
        await _redmineService.DidNotReceive().GetIssueStatusesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Close_Should_ReturnError_When_NoIssueIds()
    {
        // Arrange
        var issueIds = Array.Empty<int>();

        // Act
        var result = await _issueCommand.CloseAsync(issueIds, null, null, null, false, CancellationToken.None);

        // Assert
        result.Should().Be(1);
        await _redmineService.DidNotReceive().GetIssueStatusesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Close_Should_OutputJson_When_JsonOptionSpecified()
    {
        // Arrange
        var issueIds = new[] { 123 };
        var statuses = new List<IssueStatus>
        {
            new IssueStatus { Id = 5, Name = "Closed", IsClosed = true }
        };
        var currentIssue = new Issue
        {
            Id = 123,
            Subject = "Test Issue",
            Status = new IssueStatus { Id = 1, Name = "New", IsClosed = false }
        };
        var updatedIssue = new Issue
        {
            Id = 123,
            Subject = "Test Issue",
            Status = new IssueStatus { Id = 5, Name = "Closed", IsClosed = true },
            DoneRatio = 100
        };

        _redmineService.GetIssueStatusesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(statuses));
        _redmineService.GetIssueAsync(123, false, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(currentIssue));
        _redmineService.UpdateIssueAsync(123, null, "5", null, 100, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(updatedIssue));

        // Act
        var result = await _issueCommand.CloseAsync(issueIds, null, null, null, true, CancellationToken.None);

        // Assert
        result.Should().Be(0);
        _jsonFormatter.Received(1).FormatObject(Arg.Any<object>());
    }
}
