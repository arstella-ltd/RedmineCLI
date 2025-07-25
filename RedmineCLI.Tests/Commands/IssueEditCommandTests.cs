using System.CommandLine;

using FluentAssertions;

using Microsoft.Extensions.Logging;

using NSubstitute;

using RedmineCLI.ApiClient;
using RedmineCLI.Commands;
using RedmineCLI.Exceptions;
using RedmineCLI.Formatters;
using RedmineCLI.Models;
using RedmineCLI.Services;

using Xunit;

namespace RedmineCLI.Tests.Commands;

public class IssueEditCommandTests
{
    private readonly IRedmineApiClient _apiClient;
    private readonly IConfigService _configService;
    private readonly ITableFormatter _tableFormatter;
    private readonly IJsonFormatter _jsonFormatter;
    private readonly ILogger<IssueCommand> _logger;
    private readonly IssueCommand _issueCommand;

    public IssueEditCommandTests()
    {
        _apiClient = Substitute.For<IRedmineApiClient>();
        _configService = Substitute.For<IConfigService>();
        _tableFormatter = Substitute.For<ITableFormatter>();
        _jsonFormatter = Substitute.For<IJsonFormatter>();
        _logger = Substitute.For<ILogger<IssueCommand>>();

        _issueCommand = new IssueCommand(_apiClient, _configService, _tableFormatter, _jsonFormatter, _logger);
    }

    [Fact]
    public async Task Edit_Should_UpdateStatus_When_StatusOptionProvided()
    {
        // Arrange
        var issueId = 123;
        var newStatus = "in-progress";
        var statusList = new List<IssueStatus>
        {
            new IssueStatus { Id = 1, Name = "New" },
            new IssueStatus { Id = 2, Name = "In-Progress" },
            new IssueStatus { Id = 3, Name = "Resolved" }
        };
        var updatedIssue = new Issue
        {
            Id = issueId,
            Subject = "Test Issue",
            Status = new IssueStatus { Id = 2, Name = "In Progress" },
            Project = new Project { Id = 1, Name = "Test Project" }
        };

        var currentIssue = new Issue
        {
            Id = issueId,
            Subject = "Test Issue",
            Status = new IssueStatus { Id = 1, Name = "New" },
            Project = new Project { Id = 1, Name = "Test Project" }
        };

        _apiClient.GetIssueAsync(issueId, false, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(currentIssue));
        _apiClient.GetIssueStatusesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(statusList));
        _apiClient.UpdateIssueAsync(
            issueId,
            Arg.Is<Issue>(i => i.Status != null && i.Status.Id == 2 && i.Subject == "Test Issue"),
            Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(updatedIssue));

        // Act
        var result = await _issueCommand.EditAsync(issueId, newStatus, null, null, false, CancellationToken.None);

        // Assert
        result.Should().Be(0);
        await _apiClient.Received(1).GetIssueAsync(issueId, false, Arg.Any<CancellationToken>());
        await _apiClient.Received(1).GetIssueStatusesAsync(Arg.Any<CancellationToken>());
        await _apiClient.Received(1).UpdateIssueAsync(
            issueId,
            Arg.Is<Issue>(i => i.Status != null && i.Status.Id == 2 && i.Subject == "Test Issue"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Edit_Should_UpdateAssignee_When_AssigneeOptionProvided()
    {
        // Arrange
        var issueId = 456;
        var newAssignee = "john.doe";
        var assigneeUserId = 5;
        var users = new List<User>
        {
            new User { Id = assigneeUserId, Name = "John Doe", Login = "john.doe" }
        };
        var currentIssue = new Issue
        {
            Id = issueId,
            Subject = "Test Issue",
            Status = new IssueStatus { Id = 1, Name = "New" },
            Project = new Project { Id = 1, Name = "Test Project" }
        };
        var updatedIssue = new Issue
        {
            Id = issueId,
            Subject = "Test Issue",
            AssignedTo = users[0],
            Project = new Project { Id = 1, Name = "Test Project" }
        };

        _apiClient.GetUsersAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(users));
        _apiClient.GetIssueAsync(issueId, false, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(currentIssue));
        _apiClient.UpdateIssueAsync(
            issueId,
            Arg.Is<Issue>(i => i.AssignedTo != null && i.AssignedTo.Id == assigneeUserId && i.Subject == "Test Issue"),
            Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(updatedIssue));

        // Act
        var result = await _issueCommand.EditAsync(issueId, null, newAssignee, null, false, CancellationToken.None);

        // Assert
        result.Should().Be(0);
        await _apiClient.Received(1).GetUsersAsync(Arg.Any<CancellationToken>());
        await _apiClient.Received(1).GetIssueAsync(issueId, false, Arg.Any<CancellationToken>());
        await _apiClient.Received(1).UpdateIssueAsync(
            issueId,
            Arg.Is<Issue>(i => i.AssignedTo != null && i.AssignedTo.Id == assigneeUserId && i.Subject == "Test Issue"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Edit_Should_UpdateProgress_When_DoneRatioProvided()
    {
        // Arrange
        var issueId = 789;
        var doneRatio = 75;
        var currentIssue = new Issue
        {
            Id = issueId,
            Subject = "Test Issue",
            Status = new IssueStatus { Id = 1, Name = "New" },
            DoneRatio = 0,
            Project = new Project { Id = 1, Name = "Test Project" }
        };
        var updatedIssue = new Issue
        {
            Id = issueId,
            Subject = "Test Issue",
            DoneRatio = doneRatio,
            Project = new Project { Id = 1, Name = "Test Project" }
        };

        _apiClient.GetIssueAsync(issueId, false, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(currentIssue));
        _apiClient.UpdateIssueAsync(
            issueId,
            Arg.Is<Issue>(i => i.DoneRatio == doneRatio && i.Subject == "Test Issue"),
            Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(updatedIssue));

        // Act
        var result = await _issueCommand.EditAsync(issueId, null, null, doneRatio, false, CancellationToken.None);

        // Assert
        result.Should().Be(0);
        await _apiClient.Received(1).GetIssueAsync(issueId, false, Arg.Any<CancellationToken>());
        await _apiClient.Received(1).UpdateIssueAsync(
            issueId,
            Arg.Is<Issue>(i => i.DoneRatio == doneRatio && i.Subject == "Test Issue"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Edit_Should_ShowConfirmation_When_UpdateSuccessful()
    {
        // Arrange
        var issueId = 321;
        var newStatus = "closed";
        var statusList = new List<IssueStatus>
        {
            new IssueStatus { Id = 1, Name = "New" },
            new IssueStatus { Id = 5, Name = "Closed" }
        };
        var profile = new Profile { Url = "https://redmine.example.com", ApiKey = "test-key" };
        var updatedIssue = new Issue
        {
            Id = issueId,
            Subject = "Updated Issue",
            Status = new IssueStatus { Id = 5, Name = "Closed" },
            Project = new Project { Id = 1, Name = "Test Project" }
        };

        var currentIssue = new Issue
        {
            Id = issueId,
            Subject = "Updated Issue",
            Status = new IssueStatus { Id = 1, Name = "New" },
            Project = new Project { Id = 1, Name = "Test Project" }
        };

        _apiClient.GetIssueAsync(issueId, false, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(currentIssue));
        _apiClient.GetIssueStatusesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(statusList));
        _configService.GetActiveProfileAsync().Returns(Task.FromResult<Profile?>(profile));
        _apiClient.UpdateIssueAsync(
            issueId,
            Arg.Any<Issue>(),
            Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(updatedIssue));

        // Act
        var result = await _issueCommand.EditAsync(issueId, newStatus, null, null, false, CancellationToken.None);

        // Assert
        result.Should().Be(0);
        await _apiClient.Received(1).GetIssueAsync(issueId, false, Arg.Any<CancellationToken>());
        await _apiClient.Received(1).UpdateIssueAsync(issueId, Arg.Any<Issue>(), Arg.Any<CancellationToken>());
        await _configService.Received(1).GetActiveProfileAsync();
    }

    [Fact]
    public async Task Edit_Should_OpenBrowser_When_WebOptionIsSet()
    {
        // Arrange
        var issueId = 999;
        var profile = new Profile { Url = "https://redmine.example.com", ApiKey = "test-key" };
        _configService.GetActiveProfileAsync().Returns(Task.FromResult<Profile?>(profile));

        // Act
        var result = await _issueCommand.EditAsync(issueId, null, null, null, true, CancellationToken.None);

        // Assert
        result.Should().Be(0);
        await _configService.Received(1).GetActiveProfileAsync();
        await _apiClient.DidNotReceive().UpdateIssueAsync(Arg.Any<int>(), Arg.Any<Issue>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Edit_Should_SetAssigneeToMe_When_AssigneeIsAtMe()
    {
        // Arrange
        var issueId = 555;
        var currentUser = new User { Id = 10, Name = "Current User" };
        var currentIssue = new Issue
        {
            Id = issueId,
            Subject = "Test Issue",
            Status = new IssueStatus { Id = 1, Name = "New" },
            Project = new Project { Id = 1, Name = "Test Project" }
        };
        var updatedIssue = new Issue
        {
            Id = issueId,
            Subject = "Test Issue",
            AssignedTo = currentUser,
            Project = new Project { Id = 1, Name = "Test Project" }
        };

        _apiClient.GetIssueAsync(issueId, false, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(currentIssue));
        _apiClient.GetCurrentUserAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(currentUser));
        _apiClient.UpdateIssueAsync(
            issueId,
            Arg.Is<Issue>(i => i.AssignedTo != null && i.AssignedTo.Id == currentUser.Id && i.Subject == "Test Issue"),
            Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(updatedIssue));

        // Act
        var result = await _issueCommand.EditAsync(issueId, null, "@me", null, false, CancellationToken.None);

        // Assert
        result.Should().Be(0);
        await _apiClient.Received(1).GetIssueAsync(issueId, false, Arg.Any<CancellationToken>());
        await _apiClient.Received(1).GetCurrentUserAsync(Arg.Any<CancellationToken>());
        await _apiClient.Received(1).UpdateIssueAsync(
            issueId,
            Arg.Is<Issue>(i => i.AssignedTo != null && i.AssignedTo.Id == currentUser.Id && i.Subject == "Test Issue"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Edit_Should_UpdateMultipleFields_When_MultipleOptionsProvided()
    {
        // Arrange
        var issueId = 777;
        var newStatus = "resolved";
        var newAssignee = "jane.smith";
        var assigneeUserId = 7;
        var doneRatio = 90;
        var users = new List<User>
        {
            new User { Id = assigneeUserId, Name = "Jane Smith", Login = "jane.smith" }
        };
        var statusList = new List<IssueStatus>
        {
            new IssueStatus { Id = 1, Name = "New" },
            new IssueStatus { Id = 3, Name = "Resolved" }
        };
        var updatedIssue = new Issue
        {
            Id = issueId,
            Subject = "Test Issue",
            Status = new IssueStatus { Id = 3, Name = "Resolved" },
            AssignedTo = users[0],
            DoneRatio = doneRatio,
            Project = new Project { Id = 1, Name = "Test Project" }
        };

        var currentIssue = new Issue
        {
            Id = issueId,
            Subject = "Test Issue",
            Status = new IssueStatus { Id = 1, Name = "New" },
            Project = new Project { Id = 1, Name = "Test Project" }
        };

        _apiClient.GetUsersAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(users));
        _apiClient.GetIssueAsync(issueId, false, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(currentIssue));
        _apiClient.GetIssueStatusesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(statusList));
        _apiClient.UpdateIssueAsync(
            issueId,
            Arg.Is<Issue>(i =>
                i.Status != null && i.Status.Id == 3 &&
                i.AssignedTo != null && i.AssignedTo.Id == assigneeUserId &&
                i.DoneRatio == doneRatio &&
                i.Subject == "Test Issue"),
            Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(updatedIssue));

        // Act
        var result = await _issueCommand.EditAsync(issueId, newStatus, newAssignee, doneRatio, false, CancellationToken.None);

        // Assert
        result.Should().Be(0);
        await _apiClient.Received(1).GetUsersAsync(Arg.Any<CancellationToken>());
        await _apiClient.Received(1).GetIssueAsync(issueId, false, Arg.Any<CancellationToken>());
        await _apiClient.Received(1).UpdateIssueAsync(
            issueId,
            Arg.Is<Issue>(i =>
                i.Status != null && i.Status.Id == 3 &&
                i.AssignedTo != null && i.AssignedTo.Id == assigneeUserId &&
                i.DoneRatio == doneRatio &&
                i.Subject == "Test Issue"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Edit_Should_HandleApiError_When_UpdateFails()
    {
        // Arrange
        var issueId = 404;
        var statusList = new List<IssueStatus>
        {
            new IssueStatus { Id = 1, Name = "New" },
            new IssueStatus { Id = 5, Name = "Closed" }
        };

        var currentIssue = new Issue
        {
            Id = issueId,
            Subject = "Test Issue",
            Status = new IssueStatus { Id = 1, Name = "New" },
            Project = new Project { Id = 1, Name = "Test Project" }
        };

        _apiClient.GetIssueAsync(issueId, false, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(currentIssue));
        _apiClient.GetIssueStatusesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(statusList));
        _apiClient.UpdateIssueAsync(Arg.Any<int>(), Arg.Any<Issue>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<Issue>(new RedmineApiException(404, "Issue not found")));

        // Act
        var result = await _issueCommand.EditAsync(issueId, "closed", null, null, false, CancellationToken.None);

        // Assert
        result.Should().Be(1);
        await _apiClient.Received(1).GetIssueAsync(issueId, false, Arg.Any<CancellationToken>());
        await _apiClient.Received(1).UpdateIssueAsync(issueId, Arg.Any<Issue>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Edit_Should_ReturnError_When_WebOptionSetButNoActiveProfile()
    {
        // Arrange
        var issueId = 123;
        _configService.GetActiveProfileAsync().Returns(Task.FromResult<Profile?>(null));

        // Act
        var result = await _issueCommand.EditAsync(issueId, null, null, null, true, CancellationToken.None);

        // Assert
        result.Should().Be(1);
        await _configService.Received(1).GetActiveProfileAsync();
    }

    [Fact]
    public async Task Edit_Should_LaunchInteractiveMode_When_NoOptionsProvided()
    {
        // Arrange
        var issueId = 888;
        var originalIssue = new Issue
        {
            Id = issueId,
            Subject = "Test Issue",
            Status = new IssueStatus { Id = 1, Name = "New" },
            AssignedTo = new User { Id = 5, Name = "John Doe" },
            DoneRatio = 30,
            Project = new Project { Id = 1, Name = "Test Project" }
        };

        _apiClient.GetIssueAsync(issueId, false, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(originalIssue));

        // Act
        var result = await _issueCommand.EditAsync(issueId, null, null, null, false, CancellationToken.None);

        // Assert
        // Since this would launch interactive mode, we can't fully test it in unit tests
        // But we can verify that it attempted to fetch the issue first
        await _apiClient.Received(1).GetIssueAsync(issueId, false, Arg.Any<CancellationToken>());
    }

    [Fact]
    public void EditCommand_Should_HaveCorrectOptions_When_Created()
    {
        // Arrange & Act
        var command = IssueCommand.Create(_apiClient, _configService, _tableFormatter, _jsonFormatter, _logger);
        var editCommand = command.Subcommands.FirstOrDefault(sc => sc.Name == "edit");

        // Assert
        editCommand.Should().NotBeNull();

        var optionNames = editCommand!.Options.Select(o => o.Name).ToList();
        optionNames.Should().Contain("--status");
        optionNames.Should().Contain("--assignee");
        optionNames.Should().Contain("--done-ratio");
        optionNames.Should().Contain("--web");

        var argNames = editCommand.Arguments.Select(a => a.Name).ToList();
        argNames.Should().Contain("ID");
    }

    [Fact]
    public async Task Edit_Should_ValidateDoneRatio_When_OutOfRange()
    {
        // Arrange
        var issueId = 111;
        var invalidDoneRatio = 150; // Out of 0-100 range

        // Act
        var result = await _issueCommand.EditAsync(issueId, null, null, invalidDoneRatio, false, CancellationToken.None);

        // Assert
        result.Should().Be(1);
        await _apiClient.DidNotReceive().GetIssueAsync(Arg.Any<int>(), Arg.Any<bool>(), Arg.Any<CancellationToken>());
        await _apiClient.DidNotReceive().UpdateIssueAsync(Arg.Any<int>(), Arg.Any<Issue>(), Arg.Any<CancellationToken>());
    }
}
