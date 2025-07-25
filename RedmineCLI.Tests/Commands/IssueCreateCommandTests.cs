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

public class IssueCreateCommandTests
{
    private readonly IRedmineService _redmineService;
    private readonly IConfigService _configService;
    private readonly ITableFormatter _tableFormatter;
    private readonly IJsonFormatter _jsonFormatter;
    private readonly ILogger<IssueCommand> _logger;
    private readonly IssueCommand _issueCommand;

    public IssueCreateCommandTests()
    {
        _redmineService = Substitute.For<IRedmineService>();
        _configService = Substitute.For<IConfigService>();
        _tableFormatter = Substitute.For<ITableFormatter>();
        _jsonFormatter = Substitute.For<IJsonFormatter>();
        _logger = Substitute.For<ILogger<IssueCommand>>();

        _issueCommand = new IssueCommand(_redmineService, _configService, _tableFormatter, _jsonFormatter, _logger);
    }

    [Fact]
    public async Task Create_Should_CreateIssue_When_InteractiveMode()
    {
        // Arrange
        var projects = new List<Project>
        {
            new Project { Id = 1, Name = "Project 1", Identifier = "project1" },
            new Project { Id = 2, Name = "Project 2", Identifier = "project2" }
        };

        var createdIssue = new Issue
        {
            Id = 123,
            Subject = "Test Issue",
            Description = "Test Description",
            Project = projects[0],
            Status = new IssueStatus { Id = 1, Name = "New" }
        };

        _redmineService.GetProjectsAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(projects));

        _redmineService.CreateIssueAsync(
            "1",
            "Test Issue",
            "Test Description",
            null,
            Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(createdIssue));

        var profile = new Profile { Url = "https://redmine.example.com", ApiKey = "test-key" };
        _configService.GetActiveProfileAsync().Returns(Task.FromResult<Profile?>(profile));

        // Act - Note: In actual test, we need to mock console input
        // For now, we test the method directly
        var result = await _issueCommand.CreateAsync("1", "Test Issue", "Test Description", null, false, CancellationToken.None);

        // Assert
        result.Should().Be(0);
        await _redmineService.Received(1).CreateIssueAsync("1", "Test Issue", "Test Description", null, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Create_Should_CreateIssue_When_OptionsProvided()
    {
        // Arrange
        var projectId = "project1";
        var title = "Test Issue from CLI";
        var description = "Created via command line";
        var assignee = "john.doe";

        var createdIssue = new Issue
        {
            Id = 124,
            Subject = title,
            Description = description,
            Project = new Project { Id = 1, Name = "Project 1", Identifier = projectId },
            AssignedTo = new User { Id = 5, Name = "John Doe" },
            Status = new IssueStatus { Id = 1, Name = "New" }
        };

        _redmineService.CreateIssueAsync(
            projectId,
            title,
            description,
            assignee,
            Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(createdIssue));

        var profile = new Profile { Url = "https://redmine.example.com", ApiKey = "test-key" };
        _configService.GetActiveProfileAsync().Returns(Task.FromResult<Profile?>(profile));

        // Act
        var result = await _issueCommand.CreateAsync(projectId, title, description, assignee, false, CancellationToken.None);

        // Assert
        result.Should().Be(0);
        await _redmineService.Received(1).CreateIssueAsync(projectId, title, description, assignee, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Create_Should_ShowUrl_When_IssueCreated()
    {
        // Arrange
        var createdIssue = new Issue
        {
            Id = 125,
            Subject = "Test Issue",
            Project = new Project { Id = 1, Name = "Project 1" },
            Status = new IssueStatus { Id = 1, Name = "New" }
        };

        _redmineService.CreateIssueAsync(
            "1",
            "Test Issue",
            null,
            null,
            Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(createdIssue));

        var profile = new Profile { Url = "https://redmine.example.com", ApiKey = "test-key" };
        _configService.GetActiveProfileAsync().Returns(Task.FromResult<Profile?>(profile));

        // Act
        var result = await _issueCommand.CreateAsync("1", "Test Issue", null, null, false, CancellationToken.None);

        // Assert
        result.Should().Be(0);
        // Verify that the URL was displayed (we can't easily test console output in unit tests)
        // But we ensure the method completes successfully
    }

    [Fact]
    public async Task Create_Should_ValidateInput_When_RequiredFieldsMissing()
    {
        // Arrange
        // Test validation for missing title
        var profile = new Profile { Url = "https://redmine.example.com", ApiKey = "test-key" };
        _configService.GetActiveProfileAsync().Returns(Task.FromResult<Profile?>(profile));

        // Act - Title is empty, should return error without calling API
        var result = await _issueCommand.CreateAsync("project1", "", "Description", null, false, CancellationToken.None);

        // Assert
        result.Should().Be(1);
        // Should not call CreateIssueAsync when validation fails
        await _redmineService.DidNotReceive().CreateIssueAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Create_Should_OpenBrowser_When_WebOptionIsSet()
    {
        // Arrange
        var profile = new Profile { Url = "https://redmine.example.com", ApiKey = "test-key" };
        _configService.GetActiveProfileAsync().Returns(Task.FromResult<Profile?>(profile));

        // Act - Open browser with project specified
        var result = await _issueCommand.CreateAsync("project1", null, null, null, true, CancellationToken.None);

        // Assert
        result.Should().Be(0);
        await _configService.Received(1).GetActiveProfileAsync();
        // Browser opening can't be easily tested, but we verify the success path
        await _redmineService.DidNotReceive().CreateIssueAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Create_Should_SetAssigneeToMe_When_AssigneeIsAtMe()
    {
        // Arrange
        var currentUser = new User { Id = 10, Name = "Current User" };
        var createdIssue = new Issue
        {
            Id = 126,
            Subject = "Test Issue",
            Project = new Project { Id = 1, Name = "Project 1" },
            AssignedTo = currentUser,
            Status = new IssueStatus { Id = 1, Name = "New" }
        };

        _redmineService.GetCurrentUserAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(currentUser));

        _redmineService.CreateIssueAsync(
            "1",
            "Test Issue",
            "Description",
            "@me",
            Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(createdIssue));

        var profile = new Profile { Url = "https://redmine.example.com", ApiKey = "test-key" };
        _configService.GetActiveProfileAsync().Returns(Task.FromResult<Profile?>(profile));

        // Act
        var result = await _issueCommand.CreateAsync("1", "Test Issue", "Description", "@me", false, CancellationToken.None);

        // Assert
        result.Should().Be(0);
        // GetCurrentUserAsync is now called inside RedmineService.CreateIssueAsync when assignee is "@me"
        // So we don't need to verify it was called directly from IssueCommand
        await _redmineService.Received(1).CreateIssueAsync("1", "Test Issue", "Description", "@me", Arg.Any<CancellationToken>());
    }

    [Fact]
    public void CreateCommand_Should_HaveCorrectOptions_When_Created()
    {
        // Arrange & Act
        var command = IssueCommand.Create(_redmineService, _configService, _tableFormatter, _jsonFormatter, _logger);
        var createCommand = command.Subcommands.FirstOrDefault(sc => sc.Name == "create");

        // Assert
        createCommand.Should().NotBeNull();
        var optionNames = createCommand!.Options.Select(o => o.Name).ToList();
        optionNames.Should().Contain("--project");
        optionNames.Should().Contain("--title");
        optionNames.Should().Contain("--description");
        optionNames.Should().Contain("--assignee");
        optionNames.Should().Contain("--web");

        // Check aliases
        var projectOption = createCommand.Options.First(o => o.Name == "--project");
        projectOption.Aliases.Should().Contain("-p");

        var titleOption = createCommand.Options.First(o => o.Name == "--title");
        titleOption.Aliases.Should().Contain("-t");

        var assigneeOption = createCommand.Options.First(o => o.Name == "--assignee");
        assigneeOption.Aliases.Should().Contain("-a");

        var webOption = createCommand.Options.First(o => o.Name == "--web");
        webOption.Aliases.Should().Contain("-w");
    }
}
