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
    public void Command_Should_HaveLsAlias()
    {
        // Arrange & Act
        var command = IssueCommand.Create(_apiClient, _configService, _tableFormatter, _jsonFormatter, _logger);
        var listCommand = command.Subcommands.First(c => c.Name == "list");

        // Assert
        listCommand.Aliases.Should().Contain("ls");
    }

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
        var result = await _issueCommand.ListAsync(null, null, null, null, null, false, false, false, null, null, CancellationToken.None);

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
        var result = await _issueCommand.ListAsync(null, status, null, null, null, false, false, false, null, null, CancellationToken.None);

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
        var result = await _issueCommand.ListAsync(null, null, null, limit, offset, false, false, false, null, null, CancellationToken.None);

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
        var result = await _issueCommand.ListAsync(null, null, null, null, null, true, false, false, null, null, CancellationToken.None);

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
        var assigneeUserId = "5";
        var users = new List<User>
        {
            new User { Id = 5, Name = "John Doe", Login = "john.doe" }
        };
        var issues = new List<Issue>
        {
            new Issue
            {
                Id = 1,
                Subject = "John's Issue",
                Status = new IssueStatus { Id = 1, Name = "New" },
                AssignedTo = users[0],
                Project = new Project { Id = 1, Name = "Test Project" }
            }
        };

        _apiClient.GetUsersAsync(Arg.Any<int?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(users));
        _apiClient.GetIssuesAsync(Arg.Is<IssueFilter>(f => f.AssignedToId == assigneeUserId), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(issues));

        // Act
        var result = await _issueCommand.ListAsync(assignee, null, null, null, null, false, false, false, null, null, CancellationToken.None);

        // Assert
        result.Should().Be(0);
        await _apiClient.Received(1).GetUsersAsync(Arg.Any<int?>(), Arg.Any<CancellationToken>());
        await _apiClient.Received(1).GetIssuesAsync(
            Arg.Is<IssueFilter>(f => f.AssignedToId == assigneeUserId),
            Arg.Any<CancellationToken>());
        _tableFormatter.Received(1).FormatIssues(issues);
    }

    [Fact]
    public async Task List_Should_FilterByProject_When_ProjectIsSpecified()
    {
        // Arrange
        var project = "my-project";
        var projectIdentifier = "my-project";
        var projects = new List<Project>
        {
            new Project { Id = 10, Name = "My Project", Identifier = projectIdentifier }
        };
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

        _apiClient.GetProjectsAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(projects));
        _apiClient.GetIssuesAsync(Arg.Is<IssueFilter>(f => f.ProjectId == projectIdentifier), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(issues));

        // Act
        var result = await _issueCommand.ListAsync(null, null, project, null, null, false, false, false, null, null, CancellationToken.None);

        // Assert
        result.Should().Be(0);
        await _apiClient.Received(1).GetProjectsAsync(Arg.Any<CancellationToken>());
        await _apiClient.Received(1).GetIssuesAsync(
            Arg.Is<IssueFilter>(f => f.ProjectId == projectIdentifier),
            Arg.Any<CancellationToken>());
        _tableFormatter.Received(1).FormatIssues(issues);
    }

    [Fact]
    public async Task List_Should_FilterByProjectName_When_ProjectNameIsSpecified()
    {
        // Arrange
        var projectName = "管理"; // Using Japanese name as mentioned in the issue
        var projectIdentifier = "kanri";
        var projects = new List<Project>
        {
            new Project { Id = 10, Name = projectName, Identifier = projectIdentifier }
        };
        var issues = new List<Issue>
        {
            new Issue
            {
                Id = 1,
                Subject = "Project Issue",
                Status = new IssueStatus { Id = 1, Name = "New" },
                Project = new Project { Id = 10, Name = projectName }
            }
        };

        _apiClient.GetProjectsAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(projects));
        _apiClient.GetIssuesAsync(Arg.Is<IssueFilter>(f => f.ProjectId == projectIdentifier), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(issues));

        // Act
        var result = await _issueCommand.ListAsync(null, null, projectName, null, null, false, false, false, null, null, CancellationToken.None);

        // Assert
        result.Should().Be(0);
        await _apiClient.Received(1).GetProjectsAsync(Arg.Any<CancellationToken>());
        await _apiClient.Received(1).GetIssuesAsync(
            Arg.Is<IssueFilter>(f => f.ProjectId == projectIdentifier),
            Arg.Any<CancellationToken>());
        _tableFormatter.Received(1).FormatIssues(issues);
    }

    [Fact]
    public async Task List_Should_FilterByProjectId_When_ProjectIdIsSpecified()
    {
        // Arrange
        var projectId = "10";
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

        // When project is numeric, it should be passed as-is without resolution
        _apiClient.GetIssuesAsync(Arg.Is<IssueFilter>(f => f.ProjectId == projectId), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(issues));

        // Act
        var result = await _issueCommand.ListAsync(null, null, projectId, null, null, false, false, false, null, null, CancellationToken.None);

        // Assert
        result.Should().Be(0);
        // GetProjectsAsync should NOT be called when using numeric ID
        await _apiClient.DidNotReceive().GetProjectsAsync(Arg.Any<CancellationToken>());
        await _apiClient.Received(1).GetIssuesAsync(
            Arg.Is<IssueFilter>(f => f.ProjectId == projectId),
            Arg.Any<CancellationToken>());
        _tableFormatter.Received(1).FormatIssues(issues);
    }

    [Fact]
    public async Task List_Should_ReturnError_When_ProjectNotFound()
    {
        // Arrange
        var projectName = "NonExistentProject";
        var projects = new List<Project>(); // Empty list, project not found

        _apiClient.GetProjectsAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(projects));

        // Act
        var result = await _issueCommand.ListAsync(null, null, projectName, null, null, false, false, false, null, null, CancellationToken.None);

        // Assert
        result.Should().Be(1); // Should return error
        await _apiClient.Received(1).GetProjectsAsync(Arg.Any<CancellationToken>());
        // GetIssuesAsync should NOT be called when project is not found
        await _apiClient.DidNotReceive().GetIssuesAsync(Arg.Any<IssueFilter>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task List_Should_ApplyMultipleFilters_When_MultipleOptionsSpecified()
    {
        // Arrange
        var assignee = "john.doe";
        var assigneeUserId = "5";
        var status = "open";
        var project = "my-project";
        var projectIdentifier = "my-project";
        var limit = 20;
        var users = new List<User>
        {
            new User { Id = 5, Name = "John Doe", Login = "john.doe" }
        };
        var projects = new List<Project>
        {
            new Project { Id = 10, Name = "My Project", Identifier = projectIdentifier }
        };
        var issues = new List<Issue>
        {
            new Issue
            {
                Id = 1,
                Subject = "Filtered Issue",
                Status = new IssueStatus { Id = 1, Name = "New" },
                AssignedTo = users[0],
                Project = new Project { Id = 10, Name = "My Project" }
            }
        };

        _apiClient.GetUsersAsync(Arg.Any<int?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(users));
        _apiClient.GetProjectsAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(projects));
        _apiClient.GetIssuesAsync(
            Arg.Is<IssueFilter>(f =>
                f.AssignedToId == assigneeUserId &&
                f.StatusId == status &&
                f.ProjectId == projectIdentifier &&
                f.Limit == limit),
            Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(issues));

        // Act
        var result = await _issueCommand.ListAsync(assignee, status, project, limit, null, false, false, false, null, null, CancellationToken.None);

        // Assert
        result.Should().Be(0);
        await _apiClient.Received(1).GetUsersAsync(Arg.Any<int?>(), Arg.Any<CancellationToken>());
        await _apiClient.Received(1).GetProjectsAsync(Arg.Any<CancellationToken>());
        await _apiClient.Received(1).GetIssuesAsync(
            Arg.Is<IssueFilter>(f =>
                f.AssignedToId == assigneeUserId &&
                f.StatusId == status &&
                f.ProjectId == projectIdentifier &&
                f.Limit == limit),
            Arg.Any<CancellationToken>());
        _tableFormatter.Received(1).FormatIssues(issues);
    }

    [Fact]
    public async Task List_Should_ReturnError_When_RequestFails()
    {
        // Arrange
        _apiClient.GetIssuesAsync(Arg.Any<IssueFilter>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<List<Issue>>(new HttpRequestException("API Error")));

        // Act
        var result = await _issueCommand.ListAsync(null, null, null, null, null, false, false, false, null, null, CancellationToken.None);

        // Assert
        result.Should().Be(1);
        _tableFormatter.DidNotReceive().FormatIssues(Arg.Any<List<Issue>>());
        _jsonFormatter.DidNotReceive().FormatIssues(Arg.Any<List<Issue>>());
    }

    [Fact]
    public async Task List_Should_SearchIssues_When_SearchParameterIsProvided()
    {
        // Arrange
        var searchQuery = "会議";
        var searchResults = new List<Issue>
        {
            new Issue
            {
                Id = 1,
                Subject = "会議の準備",
                Status = new IssueStatus { Id = 1, Name = "New" },
                Project = new Project { Id = 1, Name = "Test Project" }
            },
            new Issue
            {
                Id = 2,
                Subject = "定例会議の議事録",
                Status = new IssueStatus { Id = 2, Name = "In Progress" },
                Project = new Project { Id = 1, Name = "Test Project" }
            }
        };

        _apiClient.SearchIssuesAsync(searchQuery, null, null, null, 30, null, null, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(searchResults));

        // Act
        var result = await _issueCommand.ListAsync(null, null, null, null, null, false, false, false, searchQuery, null, CancellationToken.None);

        // Assert
        result.Should().Be(0);
        await _apiClient.Received(1).SearchIssuesAsync(searchQuery, null, null, null, 30, null, null, Arg.Any<CancellationToken>());
        _tableFormatter.Received(1).FormatIssues(searchResults);
    }

    [Fact]
    public async Task List_Should_SearchWithFilters_When_SearchAndOtherParametersAreProvided()
    {
        // Arrange
        var searchQuery = "バグ";
        var assignee = "@me";
        var status = "open";
        var currentUser = new User { Id = 1, Name = "Test User" };
        var searchResults = new List<Issue>
        {
            new Issue
            {
                Id = 3,
                Subject = "バグ修正",
                Status = new IssueStatus { Id = 1, Name = "New" },
                AssignedTo = currentUser,
                Project = new Project { Id = 1, Name = "Test Project" }
            }
        };

        _apiClient.GetCurrentUserAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(currentUser));
        _apiClient.SearchIssuesAsync(searchQuery, currentUser.Id.ToString(), status, null, 30, null, null, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(searchResults));

        // Act
        var result = await _issueCommand.ListAsync(assignee, status, null, null, null, false, false, false, searchQuery, null, CancellationToken.None);

        // Assert
        result.Should().Be(0);
        await _apiClient.Received(1).GetCurrentUserAsync(Arg.Any<CancellationToken>());
        await _apiClient.Received(1).SearchIssuesAsync(searchQuery, currentUser.Id.ToString(), status, null, 30, null, null, Arg.Any<CancellationToken>());
        _tableFormatter.Received(1).FormatIssues(searchResults);
    }

    [Fact]
    public async Task List_Should_DisplayTypeField_When_SearchResultsIncludeClosedIssues()
    {
        // Arrange
        var searchQuery = "課題";
        var searchResults = new List<Issue>
        {
            new Issue
            {
                Id = 1,
                Subject = "開いている課題",
                Status = new IssueStatus { Id = 1, Name = "New" },
                Project = new Project { Id = 1, Name = "Test Project" },
                SearchResultType = "issue"
            },
            new Issue
            {
                Id = 2,
                Subject = "クローズされた課題",
                Status = new IssueStatus { Id = 5, Name = "Closed" },
                Project = new Project { Id = 1, Name = "Test Project" },
                SearchResultType = "issue-closed"
            }
        };

        _apiClient.SearchIssuesAsync(searchQuery, null, null, null, 30, null, null, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(searchResults));

        // Act
        var result = await _issueCommand.ListAsync(null, null, null, null, null, false, false, false, searchQuery, null, CancellationToken.None);

        // Assert
        result.Should().Be(0);
        await _apiClient.Received(1).SearchIssuesAsync(searchQuery, null, null, null, 30, null, null, Arg.Any<CancellationToken>());
        _tableFormatter.Received(1).FormatIssues(searchResults);

        // Verify that the search results include both issue types
        searchResults.Should().Contain(i => i.SearchResultType == "issue");
        searchResults.Should().Contain(i => i.SearchResultType == "issue-closed");
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
        var result = await _issueCommand.ListAsync("@me", null, null, null, null, false, false, false, null, null, CancellationToken.None);

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

        _apiClient.GetIssuesAsync(Arg.Is<IssueFilter>(f => f.StatusId == "*" && f.Limit == 30), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(issues));

        // Act
        var result = await _issueCommand.ListAsync(null, "all", null, null, null, false, false, false, null, null, CancellationToken.None);

        // Assert
        result.Should().Be(0);
        await _apiClient.Received(1).GetIssuesAsync(
            Arg.Is<IssueFilter>(f => f.StatusId == "*" && f.Limit == 30),
            Arg.Any<CancellationToken>());
        _tableFormatter.Received(1).FormatIssues(issues);
    }

    [Fact]
    public async Task List_Should_ValidateAndUseStatusId_When_NumericStatusProvided()
    {
        // Arrange
        var statusId = "3";
        var statuses = new List<IssueStatus>
        {
            new IssueStatus { Id = 1, Name = "New" },
            new IssueStatus { Id = 3, Name = "In Progress" },
            new IssueStatus { Id = 5, Name = "Closed" }
        };
        var issues = new List<Issue>
        {
            new Issue
            {
                Id = 1,
                Subject = "In Progress Issue",
                Status = new IssueStatus { Id = 3, Name = "In Progress" },
                Project = new Project { Id = 1, Name = "Test Project" }
            }
        };

        _apiClient.GetIssueStatusesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(statuses));
        _apiClient.GetIssuesAsync(Arg.Is<IssueFilter>(f => f.StatusId == statusId), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(issues));

        // Act
        var result = await _issueCommand.ListAsync(null, statusId, null, null, null, false, false, false, null, null, CancellationToken.None);

        // Assert
        result.Should().Be(0);
        await _apiClient.Received(1).GetIssueStatusesAsync(Arg.Any<CancellationToken>());
        await _apiClient.Received(1).GetIssuesAsync(
            Arg.Is<IssueFilter>(f => f.StatusId == statusId),
            Arg.Any<CancellationToken>());
        _tableFormatter.Received(1).FormatIssues(issues);
    }

    [Fact]
    public async Task List_Should_ResolveStatusNameToId_When_StatusNameProvided()
    {
        // Arrange
        var statusName = "In Progress";
        var expectedStatusId = "2";
        var statuses = new List<IssueStatus>
        {
            new IssueStatus { Id = 1, Name = "New" },
            new IssueStatus { Id = 2, Name = "In Progress" },
            new IssueStatus { Id = 5, Name = "Closed" }
        };
        var issues = new List<Issue>
        {
            new Issue
            {
                Id = 1,
                Subject = "In Progress Issue",
                Status = new IssueStatus { Id = 2, Name = "In Progress" },
                Project = new Project { Id = 1, Name = "Test Project" }
            }
        };

        _apiClient.GetIssueStatusesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(statuses));
        _apiClient.GetIssuesAsync(Arg.Is<IssueFilter>(f => f.StatusId == expectedStatusId), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(issues));

        // Act
        var result = await _issueCommand.ListAsync(null, statusName, null, null, null, false, false, false, null, null, CancellationToken.None);

        // Assert
        result.Should().Be(0);
        await _apiClient.Received(1).GetIssueStatusesAsync(Arg.Any<CancellationToken>());
        await _apiClient.Received(1).GetIssuesAsync(
            Arg.Is<IssueFilter>(f => f.StatusId == expectedStatusId),
            Arg.Any<CancellationToken>());
        _tableFormatter.Received(1).FormatIssues(issues);
    }

    [Fact]
    public async Task List_Should_ReturnError_When_InvalidStatusIdProvided()
    {
        // Arrange
        var invalidStatusId = "999";
        var statuses = new List<IssueStatus>
        {
            new IssueStatus { Id = 1, Name = "New" },
            new IssueStatus { Id = 2, Name = "In Progress" },
            new IssueStatus { Id = 5, Name = "Closed" }
        };

        _apiClient.GetIssueStatusesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(statuses));

        // Act
        var result = await _issueCommand.ListAsync(null, invalidStatusId, null, null, null, false, false, false, null, null, CancellationToken.None);

        // Assert
        result.Should().Be(1); // Error
        await _apiClient.Received(1).GetIssueStatusesAsync(Arg.Any<CancellationToken>());
        await _apiClient.DidNotReceive().GetIssuesAsync(Arg.Any<IssueFilter>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task List_Should_ReturnError_When_UnknownStatusNameProvided()
    {
        // Arrange
        var unknownStatus = "Unknown Status";
        var statuses = new List<IssueStatus>
        {
            new IssueStatus { Id = 1, Name = "New" },
            new IssueStatus { Id = 2, Name = "In Progress" },
            new IssueStatus { Id = 5, Name = "Closed" }
        };

        _apiClient.GetIssueStatusesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(statuses));

        // Act
        var result = await _issueCommand.ListAsync(null, unknownStatus, null, null, null, false, false, false, null, null, CancellationToken.None);

        // Assert
        result.Should().Be(1); // Error
        await _apiClient.Received(1).GetIssueStatusesAsync(Arg.Any<CancellationToken>());
        await _apiClient.DidNotReceive().GetIssuesAsync(Arg.Any<IssueFilter>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task List_Should_UseCaseInsensitiveMatching_When_StatusNameProvided()
    {
        // Arrange
        var statusName = "in progress"; // lowercase
        var expectedStatusId = "2";
        var statuses = new List<IssueStatus>
        {
            new IssueStatus { Id = 1, Name = "New" },
            new IssueStatus { Id = 2, Name = "In Progress" },
            new IssueStatus { Id = 5, Name = "Closed" }
        };
        var issues = new List<Issue>
        {
            new Issue
            {
                Id = 1,
                Subject = "In Progress Issue",
                Status = new IssueStatus { Id = 2, Name = "In Progress" },
                Project = new Project { Id = 1, Name = "Test Project" }
            }
        };

        _apiClient.GetIssueStatusesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(statuses));
        _apiClient.GetIssuesAsync(Arg.Is<IssueFilter>(f => f.StatusId == expectedStatusId), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(issues));

        // Act
        var result = await _issueCommand.ListAsync(null, statusName, null, null, null, false, false, false, null, null, CancellationToken.None);

        // Assert
        result.Should().Be(0);
        await _apiClient.Received(1).GetIssueStatusesAsync(Arg.Any<CancellationToken>());
        await _apiClient.Received(1).GetIssuesAsync(
            Arg.Is<IssueFilter>(f => f.StatusId == expectedStatusId),
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
        var result = await _issueCommand.ListAsync(null, null, null, null, null, false, true, false, null, null, CancellationToken.None);

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
        var result = await _issueCommand.ListAsync(null, null, null, null, null, false, true, false, null, null, CancellationToken.None);

        // Assert
        result.Should().Be(1);
        await _configService.Received(1).GetActiveProfileAsync();
    }

    [Fact]
    public async Task List_Should_SortBySpecifiedField_When_SortOptionProvided()
    {
        // Arrange
        var issues = new List<Issue>
        {
            new Issue { Id = 1, Subject = "Test Issue 1", UpdatedOn = DateTime.Now.AddDays(-1) },
            new Issue { Id = 2, Subject = "Test Issue 2", UpdatedOn = DateTime.Now }
        };

        _apiClient.GetIssuesAsync(
            Arg.Is<IssueFilter>(f => f.Sort == "updated_on:desc"),
            Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(issues));

        // Act
        var result = await _issueCommand.ListAsync(null, null, null, null, null, false, false, false, null, "updated_on:desc", CancellationToken.None);

        // Assert
        result.Should().Be(0);
        await _apiClient.Received(1).GetIssuesAsync(
            Arg.Is<IssueFilter>(f => f.Sort == "updated_on:desc"),
            Arg.Any<CancellationToken>());
        _tableFormatter.Received(1).FormatIssues(issues);
    }

    [Fact]
    public async Task List_Should_SortByMultipleFields_When_MultipleSortFieldsProvided()
    {
        // Arrange
        var issues = new List<Issue>
        {
            new Issue { Id = 1, Subject = "Test Issue 1", Priority = new Priority { Id = 1, Name = "High" } },
            new Issue { Id = 2, Subject = "Test Issue 2", Priority = new Priority { Id = 2, Name = "Normal" } }
        };

        _apiClient.GetIssuesAsync(
            Arg.Is<IssueFilter>(f => f.Sort == "priority:desc,id"),
            Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(issues));

        // Act
        var result = await _issueCommand.ListAsync(null, null, null, null, null, false, false, false, null, "priority:desc,id", CancellationToken.None);

        // Assert
        result.Should().Be(0);
        await _apiClient.Received(1).GetIssuesAsync(
            Arg.Is<IssueFilter>(f => f.Sort == "priority:desc,id"),
            Arg.Any<CancellationToken>());
        _tableFormatter.Received(1).FormatIssues(issues);
    }

    [Fact]
    public async Task List_Should_ReturnError_When_InvalidSortFieldProvided()
    {
        // Arrange & Act
        var result = await _issueCommand.ListAsync(null, null, null, null, null, false, false, false, null, "invalid_field", CancellationToken.None);

        // Assert
        result.Should().Be(1);
        await _apiClient.DidNotReceive().GetIssuesAsync(Arg.Any<IssueFilter>(), Arg.Any<CancellationToken>());
        _tableFormatter.DidNotReceive().FormatIssues(Arg.Any<List<Issue>>());
    }

    [Fact]
    public async Task List_Should_ReturnError_When_InvalidSortDirectionProvided()
    {
        // Arrange & Act
        var result = await _issueCommand.ListAsync(null, null, null, null, null, false, false, false, null, "id:invalid", CancellationToken.None);

        // Assert
        result.Should().Be(1);
        await _apiClient.DidNotReceive().GetIssuesAsync(Arg.Any<IssueFilter>(), Arg.Any<CancellationToken>());
        _tableFormatter.DidNotReceive().FormatIssues(Arg.Any<List<Issue>>());
    }

    [Fact]
    public async Task List_Should_ApplySortToSearchResults_When_SearchAndSortProvided()
    {
        // Arrange
        var searchQuery = "bug";
        var sort = "priority:desc";
        var issues = new List<Issue>
        {
            new Issue { Id = 1, Subject = "Bug 1", Priority = new Priority { Id = 1, Name = "High" } },
            new Issue { Id = 2, Subject = "Bug 2", Priority = new Priority { Id = 2, Name = "Normal" } }
        };

        _apiClient.SearchIssuesAsync(searchQuery, null, null, null, 30, null, sort, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(issues));

        // Act
        var result = await _issueCommand.ListAsync(null, null, null, null, null, false, false, false, searchQuery, sort, CancellationToken.None);

        // Assert
        result.Should().Be(0);
        await _apiClient.Received(1).SearchIssuesAsync(searchQuery, null, null, null, 30, null, sort, Arg.Any<CancellationToken>());
        _tableFormatter.Received(1).FormatIssues(issues);
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
        var result = await _issueCommand.ViewAsync(issueId, false, false, false, false, CancellationToken.None);

        // Assert
        result.Should().Be(0);
        await _apiClient.Received(1).GetIssueAsync(issueId, true, Arg.Any<CancellationToken>());
        _tableFormatter.Received(1).FormatIssueDetails(issue, false);
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
        var result = await _issueCommand.ViewAsync(issueId, false, false, false, false, CancellationToken.None);

        // Assert
        result.Should().Be(0);
        await _apiClient.Received(1).GetIssueAsync(issueId, true, Arg.Any<CancellationToken>());
        _tableFormatter.Received(1).FormatIssueDetails(issue, false);
    }

    [Fact]
    public async Task View_Should_ReturnError_When_IssueNotFound()
    {
        // Arrange
        var issueId = 999;
        _apiClient.GetIssueAsync(issueId, true, Arg.Any<CancellationToken>())
            .Returns(Task.FromException<Issue>(new RedmineApiException(404, "Issue not found")));

        // Act
        var result = await _issueCommand.ViewAsync(issueId, false, false, false, false, CancellationToken.None);

        // Assert
        result.Should().Be(1);
        await _apiClient.Received(1).GetIssueAsync(issueId, true, Arg.Any<CancellationToken>());
        _tableFormatter.DidNotReceive().FormatIssueDetails(Arg.Any<Issue>(), Arg.Any<bool>());
        _jsonFormatter.DidNotReceive().FormatIssueDetails(Arg.Any<Issue>(), Arg.Any<bool>());
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
        var result = await _issueCommand.ViewAsync(issueId, true, false, false, false, CancellationToken.None);

        // Assert
        result.Should().Be(0);
        await _apiClient.Received(1).GetIssueAsync(issueId, true, Arg.Any<CancellationToken>());
        _jsonFormatter.Received(1).FormatIssueDetails(issue, false);
        _tableFormatter.DidNotReceive().FormatIssueDetails(Arg.Any<Issue>(), Arg.Any<bool>());
    }

    [Fact]
    public async Task View_Should_OpenBrowser_When_WebOptionIsSet()
    {
        // Arrange
        var issueId = 123;
        var profile = new Profile { Url = "https://redmine.example.com", ApiKey = "test-key" };
        _configService.GetActiveProfileAsync().Returns(Task.FromResult<Profile?>(profile));

        // Act
        var result = await _issueCommand.ViewAsync(issueId, false, true, false, false, CancellationToken.None);

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
        var result = await _issueCommand.ViewAsync(issueId, false, true, false, false, CancellationToken.None);

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

    [Fact]
    public async Task List_Should_ResolveUsernameToId_When_AssigneeIsUsername()
    {
        // Arrange
        var assigneeName = "tanaka";
        var users = new List<User>
        {
            new User { Id = 1, Name = "Yamada Taro", Login = "yamada" },
            new User { Id = 2, Name = "Tanaka Hanako", Login = "tanaka" },
            new User { Id = 3, Name = "Suzuki Jiro", Login = "suzuki" }
        };
        var expectedUserId = "2";
        var issues = new List<Issue>
        {
            new Issue
            {
                Id = 10,
                Subject = "Tanaka's Issue",
                Status = new IssueStatus { Id = 1, Name = "New" },
                AssignedTo = users[1],
                Project = new Project { Id = 1, Name = "Test Project" }
            }
        };

        _apiClient.GetUsersAsync(Arg.Any<int?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(users));
        _apiClient.GetIssuesAsync(Arg.Is<IssueFilter>(f => f.AssignedToId == expectedUserId), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(issues));

        // Act
        var result = await _issueCommand.ListAsync(assigneeName, null, null, null, null, false, false, false, null, null, CancellationToken.None);

        // Assert
        result.Should().Be(0);
        await _apiClient.Received(1).GetUsersAsync(Arg.Any<int?>(), Arg.Any<CancellationToken>());
        await _apiClient.Received(1).GetIssuesAsync(
            Arg.Is<IssueFilter>(f => f.AssignedToId == expectedUserId),
            Arg.Any<CancellationToken>());
        _tableFormatter.Received(1).FormatIssues(issues);
    }

    [Fact]
    public async Task List_Should_PassNumericIdDirectly_When_AssigneeIsNumeric()
    {
        // Arrange
        var assigneeId = "123";
        var issues = new List<Issue>
        {
            new Issue
            {
                Id = 1,
                Subject = "User 123's Issue",
                Status = new IssueStatus { Id = 1, Name = "New" },
                AssignedTo = new User { Id = 123, Name = "Test User" },
                Project = new Project { Id = 1, Name = "Test Project" }
            }
        };

        _apiClient.GetIssuesAsync(Arg.Is<IssueFilter>(f => f.AssignedToId == assigneeId), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(issues));

        // Act
        var result = await _issueCommand.ListAsync(assigneeId, null, null, null, null, false, false, false, null, null, CancellationToken.None);

        // Assert
        result.Should().Be(0);
        await _apiClient.DidNotReceive().GetUsersAsync(Arg.Any<int?>(), Arg.Any<CancellationToken>());
        await _apiClient.Received(1).GetIssuesAsync(
            Arg.Is<IssueFilter>(f => f.AssignedToId == assigneeId),
            Arg.Any<CancellationToken>());
        _tableFormatter.Received(1).FormatIssues(issues);
    }

    [Fact]
    public async Task List_Should_ReturnError_When_UsernameNotFound()
    {
        // Arrange
        var unknownUser = "nonexistent";
        var users = new List<User>
        {
            new User { Id = 1, Name = "Yamada Taro", Login = "yamada" },
            new User { Id = 2, Name = "Tanaka Hanako", Login = "tanaka" }
        };

        _apiClient.GetUsersAsync(Arg.Any<int?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(users));

        // Act
        var result = await _issueCommand.ListAsync(unknownUser, null, null, null, null, false, false, false, null, null, CancellationToken.None);

        // Assert
        result.Should().Be(1); // Error code
        await _apiClient.Received(1).GetUsersAsync(Arg.Any<int?>(), Arg.Any<CancellationToken>());
        await _apiClient.DidNotReceive().GetIssuesAsync(Arg.Any<IssueFilter>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task List_Should_ResolveDisplayNameToId_When_AssigneeIsFullName()
    {
        // Arrange
        var assigneeDisplayName = "Tanaka Hanako";
        var users = new List<User>
        {
            new User { Id = 1, Name = "Yamada Taro", Login = "yamada", FirstName = "Taro", LastName = "Yamada" },
            new User { Id = 2, Name = "Tanaka Hanako", Login = "tanaka", FirstName = "Hanako", LastName = "Tanaka" },
            new User { Id = 3, Name = "Suzuki Jiro", Login = "suzuki", FirstName = "Jiro", LastName = "Suzuki" }
        };
        var expectedUserId = "2";
        var issues = new List<Issue>
        {
            new Issue
            {
                Id = 10,
                Subject = "Tanaka's Issue",
                Status = new IssueStatus { Id = 1, Name = "New" },
                AssignedTo = users[1],
                Project = new Project { Id = 1, Name = "Test Project" }
            }
        };

        _apiClient.GetUsersAsync(Arg.Any<int?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(users));
        _apiClient.GetIssuesAsync(Arg.Is<IssueFilter>(f => f.AssignedToId == expectedUserId), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(issues));

        // Act
        var result = await _issueCommand.ListAsync(assigneeDisplayName, null, null, null, null, false, false, false, null, null, CancellationToken.None);

        // Assert
        result.Should().Be(0);
        await _apiClient.Received(1).GetUsersAsync(Arg.Any<int?>(), Arg.Any<CancellationToken>());
        await _apiClient.Received(1).GetIssuesAsync(
            Arg.Is<IssueFilter>(f => f.AssignedToId == expectedUserId),
            Arg.Any<CancellationToken>());
        _tableFormatter.Received(1).FormatIssues(issues);
    }

    [Fact]
    public async Task List_Should_ResolveLastNameFirstNameToId_When_AssigneeIsReversedFullName()
    {
        // Arrange
        var assigneeDisplayName = "田中 花子"; // LastName FirstName format
        var users = new List<User>
        {
            new User { Id = 1, Name = "Yamada Taro", Login = "yamada", FirstName = "太郎", LastName = "山田" },
            new User { Id = 2, Name = "Tanaka Hanako", Login = "tanaka", FirstName = "花子", LastName = "田中" },
            new User { Id = 3, Name = "Suzuki Jiro", Login = "suzuki", FirstName = "次郎", LastName = "鈴木" }
        };
        var expectedUserId = "2";
        var issues = new List<Issue>
        {
            new Issue
            {
                Id = 10,
                Subject = "田中さんのチケット",
                Status = new IssueStatus { Id = 1, Name = "New" },
                AssignedTo = users[1],
                Project = new Project { Id = 1, Name = "Test Project" }
            }
        };

        _apiClient.GetUsersAsync(Arg.Any<int?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(users));
        _apiClient.GetIssuesAsync(Arg.Is<IssueFilter>(f => f.AssignedToId == expectedUserId), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(issues));

        // Act
        var result = await _issueCommand.ListAsync(assigneeDisplayName, null, null, null, null, false, false, false, null, null, CancellationToken.None);

        // Assert
        result.Should().Be(0);
        await _apiClient.Received(1).GetUsersAsync(Arg.Any<int?>(), Arg.Any<CancellationToken>());
        await _apiClient.Received(1).GetIssuesAsync(
            Arg.Is<IssueFilter>(f => f.AssignedToId == expectedUserId),
            Arg.Any<CancellationToken>());
        _tableFormatter.Received(1).FormatIssues(issues);
    }
}
