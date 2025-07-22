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

public class IssueCommandTests
{
    private readonly IRedmineApiClient _apiClient;
    private readonly IConfigService _configService;
    private readonly ITableFormatter _tableFormatter;
    private readonly IJsonFormatter _jsonFormatter;
    private readonly ILogger<IssueCommand> _logger;
    private readonly IssueCommand _issueCommand;

    public IssueCommandTests()
    {
        _apiClient = Substitute.For<IRedmineApiClient>();
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

        _issueCommand = new IssueCommand(_apiClient, _configService, _tableFormatter, _jsonFormatter, _logger);
    }

    #region List Command Tests

    [Fact]
    public async Task List_Should_ReturnAllOpenIssues_When_NoOptionsSpecified()
    {
        // Arrange
        var issues = new List<Issue>
        {
            new Issue
            {
                Id = 1,
                Subject = "Test Issue 1",
                Status = new IssueStatus { Id = 1, Name = "New" },
                AssignedTo = new User { Id = 1, Name = "User 1" },
                Project = new Project { Id = 1, Name = "Test Project" }
            },
            new Issue
            {
                Id = 2,
                Subject = "Test Issue 2",
                Status = new IssueStatus { Id = 2, Name = "In Progress" },
                AssignedTo = new User { Id = 2, Name = "User 2" },
                Project = new Project { Id = 1, Name = "Test Project" }
            },
            new Issue
            {
                Id = 3,
                Subject = "Test Issue 3",
                Status = new IssueStatus { Id = 1, Name = "New" },
                AssignedTo = null,
                Project = new Project { Id = 2, Name = "Another Project" }
            }
        };

        _apiClient.GetIssuesAsync(Arg.Is<IssueFilter>(f => f.StatusId == "open" && f.AssignedToId == null && f.ProjectId == null && f.Limit == 30), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(issues));

        // Act
        var result = await _issueCommand.ListAsync(null, null, null, null, null, false, false, false, CancellationToken.None);

        // Assert
        result.Should().Be(0);
        await _apiClient.DidNotReceive().GetCurrentUserAsync(Arg.Any<CancellationToken>());
        await _apiClient.Received(1).GetIssuesAsync(
            Arg.Is<IssueFilter>(f => f.StatusId == "open" && f.AssignedToId == null && f.ProjectId == null && f.Limit == 30),
            Arg.Any<CancellationToken>());
        _tableFormatter.Received(1).FormatIssues(issues);
    }

    [Fact]
    public async Task List_Should_ReturnFilteredIssues_When_StatusIsSpecified()
    {
        // Arrange
        var status = "open";
        var issues = new List<Issue>
        {
            new Issue
            {
                Id = 1,
                Subject = "Open Issue 1",
                Status = new IssueStatus { Id = 1, Name = "New" },
                Project = new Project { Id = 1, Name = "Test Project" }
            },
            new Issue
            {
                Id = 2,
                Subject = "Open Issue 2",
                Status = new IssueStatus { Id = 2, Name = "In Progress" },
                Project = new Project { Id = 1, Name = "Test Project" }
            }
        };

        _apiClient.GetIssuesAsync(Arg.Is<IssueFilter>(f => f.StatusId == status), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(issues));

        // Act
        var result = await _issueCommand.ListAsync(null, status, null, null, null, false, false, false, CancellationToken.None);

        // Assert
        result.Should().Be(0);
        await _apiClient.Received(1).GetIssuesAsync(
            Arg.Is<IssueFilter>(f => f.StatusId == status && f.AssignedToId == null),
            Arg.Any<CancellationToken>());
        _tableFormatter.Received(1).FormatIssues(issues);
    }

    [Fact]
    public async Task List_Should_LimitResults_When_LimitOptionIsSet()
    {
        // Arrange
        var limit = 10;
        var offset = 5;
        var issues = new List<Issue>
        {
            new Issue
            {
                Id = 1,
                Subject = "Test Issue",
                Status = new IssueStatus { Id = 1, Name = "New" },
                Project = new Project { Id = 1, Name = "Test Project" }
            }
        };

        _apiClient.GetIssuesAsync(
            Arg.Is<IssueFilter>(f => f.Limit == limit && f.Offset == offset && f.StatusId == "open" && f.AssignedToId == null),
            Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(issues));

        // Act
        var result = await _issueCommand.ListAsync(null, null, null, limit, offset, false, false, false, CancellationToken.None);

        // Assert
        result.Should().Be(0);
        await _apiClient.DidNotReceive().GetCurrentUserAsync(Arg.Any<CancellationToken>());
        await _apiClient.Received(1).GetIssuesAsync(
            Arg.Is<IssueFilter>(f => f.Limit == limit && f.Offset == offset && f.StatusId == "open" && f.AssignedToId == null),
            Arg.Any<CancellationToken>());
        _tableFormatter.Received(1).FormatIssues(issues);
    }

    [Fact]
    public async Task List_Should_FormatAsJson_When_JsonOptionIsSet()
    {
        // Arrange
        var issues = new List<Issue>
        {
            new Issue
            {
                Id = 1,
                Subject = "Test Issue",
                Status = new IssueStatus { Id = 1, Name = "New" },
                Project = new Project { Id = 1, Name = "Test Project" }
            }
        };

        _apiClient.GetIssuesAsync(Arg.Is<IssueFilter>(f => f.StatusId == "open" && f.AssignedToId == null && f.Limit == 30), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(issues));

        // Act
        var result = await _issueCommand.ListAsync(null, null, null, null, null, true, false, false, CancellationToken.None);

        // Assert
        result.Should().Be(0);
        await _apiClient.DidNotReceive().GetCurrentUserAsync(Arg.Any<CancellationToken>());
        await _apiClient.Received(1).GetIssuesAsync(Arg.Is<IssueFilter>(f => f.StatusId == "open" && f.AssignedToId == null && f.Limit == 30), Arg.Any<CancellationToken>());
        _jsonFormatter.Received(1).FormatIssues(issues);
        _tableFormatter.DidNotReceive().FormatIssues(Arg.Any<List<Issue>>());
    }

    [Fact]
    public async Task List_Should_FilterByAssignee_When_AssigneeIsSpecified()
    {
        // Arrange
        var assignee = "john.doe";
        var issues = new List<Issue>
        {
            new Issue
            {
                Id = 1,
                Subject = "John's Issue",
                Status = new IssueStatus { Id = 1, Name = "New" },
                AssignedTo = new User { Id = 5, Name = "John Doe" },
                Project = new Project { Id = 1, Name = "Test Project" }
            }
        };

        _apiClient.GetIssuesAsync(Arg.Is<IssueFilter>(f => f.AssignedToId == assignee), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(issues));

        // Act
        var result = await _issueCommand.ListAsync(assignee, null, null, null, null, false, false, false, CancellationToken.None);

        // Assert
        result.Should().Be(0);
        await _apiClient.Received(1).GetIssuesAsync(
            Arg.Is<IssueFilter>(f => f.AssignedToId == assignee),
            Arg.Any<CancellationToken>());
        _tableFormatter.Received(1).FormatIssues(issues);
    }

    [Fact]
    public async Task List_Should_FilterByProject_When_ProjectIsSpecified()
    {
        // Arrange
        var project = "my-project";
        var issues = new List<Issue>
        {
            new Issue
            {
                Id = 1,
                Subject = "Project Issue",
                Status = new IssueStatus { Id = 1, Name = "New" },
                Project = new Project { Id = 10, Name = "My Project" }
            }
        };

        _apiClient.GetIssuesAsync(Arg.Is<IssueFilter>(f => f.ProjectId == project), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(issues));

        // Act
        var result = await _issueCommand.ListAsync(null, null, project, null, null, false, false, false, CancellationToken.None);

        // Assert
        result.Should().Be(0);
        await _apiClient.Received(1).GetIssuesAsync(
            Arg.Is<IssueFilter>(f => f.ProjectId == project),
            Arg.Any<CancellationToken>());
        _tableFormatter.Received(1).FormatIssues(issues);
    }

    [Fact]
    public async Task List_Should_ApplyMultipleFilters_When_MultipleOptionsSpecified()
    {
        // Arrange
        var assignee = "john.doe";
        var status = "open";
        var project = "my-project";
        var limit = 20;
        var issues = new List<Issue>
        {
            new Issue
            {
                Id = 1,
                Subject = "Filtered Issue",
                Status = new IssueStatus { Id = 1, Name = "New" },
                AssignedTo = new User { Id = 5, Name = "John Doe" },
                Project = new Project { Id = 10, Name = "My Project" }
            }
        };

        _apiClient.GetIssuesAsync(
            Arg.Is<IssueFilter>(f =>
                f.AssignedToId == assignee &&
                f.StatusId == status &&
                f.ProjectId == project &&
                f.Limit == limit),
            Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(issues));

        // Act
        var result = await _issueCommand.ListAsync(assignee, status, project, limit, null, false, false, false, CancellationToken.None);

        // Assert
        result.Should().Be(0);
        await _apiClient.Received(1).GetIssuesAsync(
            Arg.Is<IssueFilter>(f =>
                f.AssignedToId == assignee &&
                f.StatusId == status &&
                f.ProjectId == project &&
                f.Limit == limit),
            Arg.Any<CancellationToken>());
        _tableFormatter.Received(1).FormatIssues(issues);
    }

    [Fact]
    public async Task List_Should_HandleApiError_When_RequestFails()
    {
        // Arrange
        _apiClient.GetIssuesAsync(Arg.Any<IssueFilter>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<List<Issue>>(new HttpRequestException("API Error")));

        // Act
        var result = await _issueCommand.ListAsync(null, null, null, null, null, false, false, false, CancellationToken.None);

        // Assert
        result.Should().Be(1);
        _tableFormatter.DidNotReceive().FormatIssues(Arg.Any<List<Issue>>());
        _jsonFormatter.DidNotReceive().FormatIssues(Arg.Any<List<Issue>>());
    }

    #endregion

    #region Command Line Integration Tests

    [Fact]
    public void IssueCommand_Should_RegisterSubcommands_When_Created()
    {
        // Arrange & Act
        var command = IssueCommand.Create(_apiClient, _configService, _tableFormatter, _jsonFormatter, _logger);

        // Assert
        command.Should().NotBeNull();
        command.Name.Should().Be("issue");
        command.Subcommands.Should().Contain(sc => sc.Name == "list");
    }

    [Fact]
    public void ListCommand_Should_HaveCorrectOptions_When_Created()
    {
        // Arrange & Act
        var command = IssueCommand.Create(_apiClient, _configService, _tableFormatter, _jsonFormatter, _logger);
        var listCommand = command.Subcommands.First(sc => sc.Name == "list");

        // Assert
        listCommand.Should().NotBeNull();
        var optionNames = listCommand.Options.Select(o => o.Name).ToList();
        optionNames.Should().Contain("--assignee");
        optionNames.Should().Contain("--status");
        optionNames.Should().Contain("--project");
        optionNames.Should().Contain("--limit");
        optionNames.Should().Contain("--offset");
        optionNames.Should().Contain("--json");
        optionNames.Should().Contain("--absolute-time");
    }

    [Fact]
    public async Task List_Should_UseCurrentUser_When_AssigneeIsAtMe()
    {
        // Arrange
        var currentUser = new User { Id = 1, Name = "Test User" };
        var issues = new List<Issue>
        {
            new Issue
            {
                Id = 1,
                Subject = "My Issue",
                Status = new IssueStatus { Id = 1, Name = "New" },
                AssignedTo = currentUser,
                Project = new Project { Id = 1, Name = "Test Project" }
            }
        };

        _apiClient.GetCurrentUserAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(currentUser));
        _apiClient.GetIssuesAsync(Arg.Is<IssueFilter>(f => f.AssignedToId == currentUser.Id.ToString()), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(issues));

        // Act
        var result = await _issueCommand.ListAsync("@me", null, null, null, null, false, false, false, CancellationToken.None);

        // Assert
        result.Should().Be(0);
        await _apiClient.Received(1).GetCurrentUserAsync(Arg.Any<CancellationToken>());
        await _apiClient.Received(1).GetIssuesAsync(
            Arg.Is<IssueFilter>(f => f.AssignedToId == currentUser.Id.ToString()),
            Arg.Any<CancellationToken>());
        _tableFormatter.Received(1).FormatIssues(issues);
    }

    [Fact]
    public async Task List_Should_ShowAllStatuses_When_StatusIsAll()
    {
        // Arrange
        var issues = new List<Issue>
        {
            new Issue
            {
                Id = 1,
                Subject = "Open Issue",
                Status = new IssueStatus { Id = 1, Name = "New" },
                Project = new Project { Id = 1, Name = "Test Project" }
            },
            new Issue
            {
                Id = 2,
                Subject = "Closed Issue",
                Status = new IssueStatus { Id = 5, Name = "Closed" },
                Project = new Project { Id = 1, Name = "Test Project" }
            }
        };

        _apiClient.GetIssuesAsync(Arg.Is<IssueFilter>(f => f.StatusId == null && f.Limit == 30), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(issues));

        // Act
        var result = await _issueCommand.ListAsync(null, "all", null, null, null, false, false, false, CancellationToken.None);

        // Assert
        result.Should().Be(0);
        await _apiClient.Received(1).GetIssuesAsync(
            Arg.Is<IssueFilter>(f => f.StatusId == null && f.Limit == 30),
            Arg.Any<CancellationToken>());
        _tableFormatter.Received(1).FormatIssues(issues);
    }

    [Fact]
    public async Task List_Should_OpenBrowser_When_WebOptionIsSet()
    {
        // Arrange
        var profile = new Profile { Url = "https://redmine.example.com", ApiKey = "test-key" };
        _configService.GetActiveProfileAsync().Returns(Task.FromResult<Profile?>(profile));

        // Act
        var result = await _issueCommand.ListAsync(null, null, null, null, null, false, true, false, CancellationToken.None);

        // Assert
        result.Should().Be(0);
        await _configService.Received(1).GetActiveProfileAsync();
        // Note: We can't easily test browser opening in unit tests, but we verify the success path
    }

    [Fact]
    public async Task List_Should_ReturnError_When_WebOptionSetButNoActiveProfile()
    {
        // Arrange
        _configService.GetActiveProfileAsync().Returns(Task.FromResult<Profile?>(null));

        // Act
        var result = await _issueCommand.ListAsync(null, null, null, null, null, false, true, false, CancellationToken.None);

        // Assert
        result.Should().Be(1);
        await _configService.Received(1).GetActiveProfileAsync();
    }

    #endregion

    #region View Command Tests

    [Fact]
    public async Task View_Should_ShowIssueDetails_When_ValidIdProvided()
    {
        // Arrange
        var issueId = 123;
        var issue = new Issue
        {
            Id = issueId,
            Subject = "Test Issue",
            Description = "This is a test issue description",
            Status = new IssueStatus { Id = 1, Name = "New" },
            Priority = new Priority { Id = 2, Name = "Normal" },
            AssignedTo = new User { Id = 1, Name = "John Doe" },
            Project = new Project { Id = 10, Name = "Test Project" },
            CreatedOn = new DateTime(2024, 1, 1, 10, 0, 0),
            UpdatedOn = new DateTime(2024, 1, 2, 15, 30, 0),
            DoneRatio = 50,
            Journals = new List<Journal>()
        };

        _apiClient.GetIssueAsync(issueId, true, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(issue));

        // Act
        var result = await _issueCommand.ViewAsync(issueId, false, false, false, CancellationToken.None);

        // Assert
        result.Should().Be(0);
        await _apiClient.Received(1).GetIssueAsync(issueId, true, Arg.Any<CancellationToken>());
        _tableFormatter.Received(1).FormatIssueDetails(issue);
    }

    [Fact]
    public async Task View_Should_ShowHistory_When_JournalsExist()
    {
        // Arrange
        var issueId = 123;
        var issue = new Issue
        {
            Id = issueId,
            Subject = "Test Issue with History",
            Description = "Issue with journals",
            Status = new IssueStatus { Id = 1, Name = "New" },
            Project = new Project { Id = 10, Name = "Test Project" },
            CreatedOn = new DateTime(2024, 1, 1, 10, 0, 0),
            UpdatedOn = new DateTime(2024, 1, 3, 16, 45, 0),
            Journals = new List<Journal>
            {
                new Journal
                {
                    Id = 1,
                    User = new User { Id = 2, Name = "Jane Smith" },
                    Notes = "Updated the status",
                    CreatedOn = new DateTime(2024, 1, 2, 14, 0, 0),
                    Details = new List<JournalDetail>
                    {
                        new JournalDetail
                        {
                            Property = "attr",
                            Name = "status_id",
                            OldValue = "1",
                            NewValue = "2"
                        }
                    }
                },
                new Journal
                {
                    Id = 2,
                    User = new User { Id = 3, Name = "Bob Johnson" },
                    Notes = "Added a comment",
                    CreatedOn = new DateTime(2024, 1, 3, 16, 45, 0),
                    Details = null
                }
            }
        };

        _apiClient.GetIssueAsync(issueId, true, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(issue));

        // Act
        var result = await _issueCommand.ViewAsync(issueId, false, false, false, CancellationToken.None);

        // Assert
        result.Should().Be(0);
        await _apiClient.Received(1).GetIssueAsync(issueId, true, Arg.Any<CancellationToken>());
        _tableFormatter.Received(1).FormatIssueDetails(issue);
    }

    [Fact]
    public async Task View_Should_ReturnError_When_IssueNotFound()
    {
        // Arrange
        var issueId = 999;
        _apiClient.GetIssueAsync(issueId, true, Arg.Any<CancellationToken>())
            .Returns(Task.FromException<Issue>(new RedmineApiException(404, "Issue not found")));

        // Act
        var result = await _issueCommand.ViewAsync(issueId, false, false, false, CancellationToken.None);

        // Assert
        result.Should().Be(1);
        await _apiClient.Received(1).GetIssueAsync(issueId, true, Arg.Any<CancellationToken>());
        _tableFormatter.DidNotReceive().FormatIssueDetails(Arg.Any<Issue>());
        _jsonFormatter.DidNotReceive().FormatIssueDetails(Arg.Any<Issue>());
    }

    [Fact]
    public async Task View_Should_FormatAsJson_When_JsonOptionIsSet()
    {
        // Arrange
        var issueId = 123;
        var issue = new Issue
        {
            Id = issueId,
            Subject = "Test Issue",
            Description = "JSON format test",
            Status = new IssueStatus { Id = 1, Name = "New" },
            Project = new Project { Id = 10, Name = "Test Project" },
            CreatedOn = new DateTime(2024, 1, 1, 10, 0, 0),
            UpdatedOn = new DateTime(2024, 1, 2, 15, 30, 0),
            Journals = new List<Journal>()
        };

        _apiClient.GetIssueAsync(issueId, true, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(issue));

        // Act
        var result = await _issueCommand.ViewAsync(issueId, true, false, false, CancellationToken.None);

        // Assert
        result.Should().Be(0);
        await _apiClient.Received(1).GetIssueAsync(issueId, true, Arg.Any<CancellationToken>());
        _jsonFormatter.Received(1).FormatIssueDetails(issue);
        _tableFormatter.DidNotReceive().FormatIssueDetails(Arg.Any<Issue>());
    }

    [Fact]
    public async Task View_Should_OpenBrowser_When_WebOptionIsSet()
    {
        // Arrange
        var issueId = 123;
        var profile = new Profile { Url = "https://redmine.example.com", ApiKey = "test-key" };
        _configService.GetActiveProfileAsync().Returns(Task.FromResult<Profile?>(profile));

        // Act
        var result = await _issueCommand.ViewAsync(issueId, false, true, false, CancellationToken.None);

        // Assert
        result.Should().Be(0);
        await _configService.Received(1).GetActiveProfileAsync();
        // Note: We can't easily test browser opening in unit tests, but we verify the success path
        await _apiClient.DidNotReceive().GetIssueAsync(Arg.Any<int>(), Arg.Any<bool>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task View_Should_ReturnError_When_WebOptionSetButNoActiveProfile()
    {
        // Arrange
        var issueId = 123;
        _configService.GetActiveProfileAsync().Returns(Task.FromResult<Profile?>(null));

        // Act
        var result = await _issueCommand.ViewAsync(issueId, false, true, false, CancellationToken.None);

        // Assert
        result.Should().Be(1);
        await _configService.Received(1).GetActiveProfileAsync();
    }

    [Fact]
    public void ViewCommand_Should_HaveCorrectOptions_When_Created()
    {
        // Arrange & Act
        var command = IssueCommand.Create(_apiClient, _configService, _tableFormatter, _jsonFormatter, _logger);
        var viewCommand = command.Subcommands.FirstOrDefault(sc => sc.Name == "view");

        // Assert
        viewCommand.Should().NotBeNull();
        var optionNames = viewCommand!.Options.Select(o => o.Name).ToList();
        optionNames.Should().Contain("--json");
        optionNames.Should().Contain("--web");
        optionNames.Should().Contain("--absolute-time");

        var argNames = viewCommand.Arguments.Select(a => a.Name).ToList();
        argNames.Should().Contain("ID");
    }

    #endregion
}
