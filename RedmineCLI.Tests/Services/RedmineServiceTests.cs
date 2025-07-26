using System.Globalization;

using FluentAssertions;

using Microsoft.Extensions.Logging;

using NSubstitute;
using NSubstitute.ExceptionExtensions;

using RedmineCLI.ApiClient;
using RedmineCLI.Exceptions;
using RedmineCLI.Models;
using RedmineCLI.Services;

using Xunit;

namespace RedmineCLI.Tests.Services;

public class RedmineServiceTests
{
    private readonly IRedmineApiClient _mockApiClient;
    private readonly ILogger<RedmineService> _mockLogger;
    private readonly RedmineService _sut;

    public RedmineServiceTests()
    {
        _mockApiClient = Substitute.For<IRedmineApiClient>();
        _mockLogger = Substitute.For<ILogger<RedmineService>>();
        _sut = new RedmineService(_mockApiClient, _mockLogger);
    }

    [Fact]
    public async Task GetIssuesAsync_Should_ReturnIssues_When_Called()
    {
        // Arrange
        var filter = new IssueFilter { StatusId = "open" };
        var expectedIssues = new List<Issue>
        {
            new() { Id = 1, Subject = "Test Issue 1" },
            new() { Id = 2, Subject = "Test Issue 2" }
        };
        _mockApiClient.GetIssuesAsync(filter, Arg.Any<CancellationToken>())
            .Returns(expectedIssues);

        // Act
        var result = await _sut.GetIssuesAsync(filter);

        // Assert
        result.Should().BeEquivalentTo(expectedIssues);
        await _mockApiClient.Received(1).GetIssuesAsync(filter, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetIssuesAsync_Should_ResolveAtMe_When_AssignedToIsAtMe()
    {
        // Arrange
        var filter = new IssueFilter { AssignedToId = "@me" };
        var currentUser = new User { Id = 123, Name = "Test User" };
        var expectedIssues = new List<Issue> { new() { Id = 1, Subject = "My Issue" } };

        _mockApiClient.GetCurrentUserAsync(Arg.Any<CancellationToken>())
            .Returns(currentUser);
        _mockApiClient.GetIssuesAsync(
            Arg.Is<IssueFilter>(f => f.AssignedToId == "123"),
            Arg.Any<CancellationToken>())
            .Returns(expectedIssues);

        // Act
        var result = await _sut.GetIssuesAsync(filter);

        // Assert
        result.Should().BeEquivalentTo(expectedIssues);
        await _mockApiClient.Received(1).GetCurrentUserAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateIssueAsync_Should_CreateIssue_When_ValidDataProvided()
    {
        // Arrange
        const string projectIdentifier = "test-project";
        const string subject = "New Issue";
        const string description = "Issue description";
        var projects = new List<Project>
        {
            new() { Id = 10, Identifier = projectIdentifier, Name = "Test Project" }
        };
        var expectedIssue = new Issue { Id = 100, Subject = subject };

        _mockApiClient.GetProjectsAsync(Arg.Any<CancellationToken>())
            .Returns(projects);
        _mockApiClient.CreateIssueAsync(
            Arg.Is<Issue>(i => i.Subject == subject && i.Project!.Id == 10),
            Arg.Any<CancellationToken>())
            .Returns(expectedIssue);

        // Act
        var result = await _sut.CreateIssueAsync(projectIdentifier, subject, description);

        // Assert
        result.Should().BeEquivalentTo(expectedIssue);
        await _mockApiClient.Received(1).CreateIssueAsync(
            Arg.Is<Issue>(i => i.Subject == subject && i.Description == description),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateIssueAsync_Should_ResolveAtMe_When_AssigneeIsAtMe()
    {
        // Arrange
        const string projectId = "1";
        const string subject = "New Issue";
        const string assignee = "@me";
        var currentUser = new User { Id = 123, Name = "Test User" };
        var expectedIssue = new Issue { Id = 100, Subject = subject };

        _mockApiClient.GetCurrentUserAsync(Arg.Any<CancellationToken>())
            .Returns(currentUser);
        _mockApiClient.CreateIssueAsync(
            Arg.Is<Issue>(i => i.AssignedTo!.Id == 123),
            Arg.Any<CancellationToken>())
            .Returns(expectedIssue);

        // Act
        var result = await _sut.CreateIssueAsync(projectId, subject, null, assignee);

        // Assert
        result.Should().BeEquivalentTo(expectedIssue);
        await _mockApiClient.Received(1).GetCurrentUserAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateIssueAsync_Should_UpdateIssue_When_ValidDataProvided()
    {
        // Arrange
        const int issueId = 1;
        const string newSubject = "Updated Subject";
        var existingIssue = new Issue { Id = issueId, Subject = "Original Subject" };
        var updatedIssue = new Issue { Id = issueId, Subject = newSubject };

        _mockApiClient.GetIssueAsync(issueId, Arg.Any<CancellationToken>())
            .Returns(existingIssue);
        _mockApiClient.UpdateIssueAsync(
            issueId,
            Arg.Is<Issue>(i => i.Subject == newSubject),
            Arg.Any<CancellationToken>())
            .Returns(updatedIssue);

        // Act
        var result = await _sut.UpdateIssueAsync(issueId, newSubject);

        // Assert
        result.Should().BeEquivalentTo(updatedIssue);
        await _mockApiClient.Received(1).UpdateIssueAsync(
            issueId,
            Arg.Is<Issue>(i => i.Subject == newSubject),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateIssueAsync_Should_ResolveStatusName_When_StatusNameProvided()
    {
        // Arrange
        const int issueId = 1;
        const string statusName = "In Progress";
        var existingIssue = new Issue { Id = issueId, Subject = "Test Issue" };
        var statuses = new List<IssueStatus>
        {
            new() { Id = 1, Name = "New" },
            new() { Id = 2, Name = "In Progress" },
            new() { Id = 3, Name = "Closed" }
        };
        var updatedIssue = new Issue { Id = issueId, Subject = "Test Issue", Status = statuses[1] };

        _mockApiClient.GetIssueAsync(issueId, Arg.Any<CancellationToken>())
            .Returns(existingIssue);
        _mockApiClient.GetIssueStatusesAsync(Arg.Any<CancellationToken>())
            .Returns(statuses);
        _mockApiClient.UpdateIssueAsync(
            issueId,
            Arg.Is<Issue>(i => i.Status!.Id == 2),
            Arg.Any<CancellationToken>())
            .Returns(updatedIssue);

        // Act
        var result = await _sut.UpdateIssueAsync(issueId, statusIdOrName: statusName);

        // Assert
        result.Should().BeEquivalentTo(updatedIssue);
        await _mockApiClient.Received(1).GetIssueStatusesAsync(Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(101)]
    public async Task UpdateIssueAsync_Should_ThrowValidationException_When_DoneRatioIsInvalid(int invalidDoneRatio)
    {
        // Arrange
        const int issueId = 1;
        var existingIssue = new Issue { Id = issueId, Subject = "Test Issue" };
        _mockApiClient.GetIssueAsync(issueId, Arg.Any<CancellationToken>())
            .Returns(existingIssue);

        // Act
        var act = async () => await _sut.UpdateIssueAsync(issueId, doneRatio: invalidDoneRatio);

        // Assert
        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("Progress must be between 0 and 100");
    }

    [Fact]
    public async Task ResolveAssigneeAsync_Should_ReturnEmpty_When_AssigneeIsNullOrEmpty()
    {
        // Arrange & Act
        var result1 = await _sut.ResolveAssigneeAsync(null);
        var result2 = await _sut.ResolveAssigneeAsync(string.Empty);

        // Assert
        result1.Should().BeEmpty();
        result2.Should().BeEmpty();
    }

    [Fact]
    public async Task ResolveAssigneeAsync_Should_ReturnSameValue_When_AssigneeIsNumeric()
    {
        // Arrange
        const string numericAssignee = "123";

        // Act
        var result = await _sut.ResolveAssigneeAsync(numericAssignee);

        // Assert
        result.Should().Be(numericAssignee);
    }

    [Fact]
    public async Task ResolveAssigneeAsync_Should_ReturnCurrentUserId_When_AssigneeIsAtMe()
    {
        // Arrange
        var currentUser = new User { Id = 456, Name = "Current User" };
        _mockApiClient.GetCurrentUserAsync(Arg.Any<CancellationToken>())
            .Returns(currentUser);

        // Act
        var result = await _sut.ResolveAssigneeAsync("@me");

        // Assert
        result.Should().Be("456");
        await _mockApiClient.Received(1).GetCurrentUserAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ResolveStatusIdAsync_Should_ReturnNull_When_StatusIsNullOrEmpty()
    {
        // Arrange & Act
        var result1 = await _sut.ResolveStatusIdAsync(null);
        var result2 = await _sut.ResolveStatusIdAsync(string.Empty);

        // Assert
        result1.Should().BeNull();
        result2.Should().BeNull();
    }

    [Fact]
    public async Task ResolveStatusIdAsync_Should_ReturnSameValue_When_StatusIsNumeric()
    {
        // Arrange
        const string numericStatus = "5";

        // Act
        var result = await _sut.ResolveStatusIdAsync(numericStatus);

        // Assert
        result.Should().Be(5);
    }

    [Fact]
    public async Task ResolveStatusIdAsync_Should_ReturnStatusId_When_StatusNameProvided()
    {
        // Arrange
        const string statusName = "closed";
        var statuses = new List<IssueStatus>
        {
            new() { Id = 1, Name = "New" },
            new() { Id = 2, Name = "In Progress" },
            new() { Id = 3, Name = "Closed" }
        };
        _mockApiClient.GetIssueStatusesAsync(Arg.Any<CancellationToken>())
            .Returns(statuses);

        // Act
        var result = await _sut.ResolveStatusIdAsync(statusName);

        // Assert
        result.Should().Be(3);
    }

    [Fact]
    public async Task ResolveStatusIdAsync_Should_ThrowValidationException_When_StatusNotFound()
    {
        // Arrange
        const string unknownStatus = "Unknown";
        var statuses = new List<IssueStatus>
        {
            new() { Id = 1, Name = "New" },
            new() { Id = 2, Name = "In Progress" }
        };
        _mockApiClient.GetIssueStatusesAsync(Arg.Any<CancellationToken>())
            .Returns(statuses);

        // Act
        var act = async () => await _sut.ResolveStatusIdAsync(unknownStatus);

        // Assert
        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage($"Status '{unknownStatus}' not found");
    }

    [Fact]
    public async Task ResolveProjectIdAsync_Should_ThrowValidationException_When_ProjectIsNullOrEmpty()
    {
        // Arrange & Act
        var act1 = async () => await _sut.ResolveProjectIdAsync(null!);
        var act2 = async () => await _sut.ResolveProjectIdAsync(string.Empty);

        // Assert
        await act1.Should().ThrowAsync<ValidationException>()
            .WithMessage("Project ID or identifier is required");
        await act2.Should().ThrowAsync<ValidationException>()
            .WithMessage("Project ID or identifier is required");
    }

    [Fact]
    public async Task ResolveProjectIdAsync_Should_ReturnSameValue_When_ProjectIsNumeric()
    {
        // Arrange
        const string numericProject = "10";

        // Act
        var result = await _sut.ResolveProjectIdAsync(numericProject);

        // Assert
        result.Should().Be(10);
    }

    [Fact]
    public async Task ResolveProjectIdAsync_Should_ReturnProjectId_When_IdentifierProvided()
    {
        // Arrange
        const string projectIdentifier = "test-proj";
        var projects = new List<Project>
        {
            new() { Id = 1, Identifier = "main-proj", Name = "Main Project" },
            new() { Id = 2, Identifier = projectIdentifier, Name = "Test Project" }
        };
        _mockApiClient.GetProjectsAsync(Arg.Any<CancellationToken>())
            .Returns(projects);

        // Act
        var result = await _sut.ResolveProjectIdAsync(projectIdentifier);

        // Assert
        result.Should().Be(2);
    }

    [Fact]
    public async Task ResolveProjectIdAsync_Should_ReturnProjectId_When_NameProvided()
    {
        // Arrange
        const string projectName = "Test Project";
        var projects = new List<Project>
        {
            new() { Id = 1, Identifier = "main-proj", Name = "Main Project" },
            new() { Id = 2, Identifier = "test-proj", Name = projectName }
        };
        _mockApiClient.GetProjectsAsync(Arg.Any<CancellationToken>())
            .Returns(projects);

        // Act
        var result = await _sut.ResolveProjectIdAsync(projectName);

        // Assert
        result.Should().Be(2);
    }

    [Fact]
    public async Task ResolveProjectIdAsync_Should_ThrowValidationException_When_ProjectNotFound()
    {
        // Arrange
        const string unknownProject = "Unknown";
        var projects = new List<Project>
        {
            new() { Id = 1, Identifier = "proj1", Name = "Project 1" }
        };
        _mockApiClient.GetProjectsAsync(Arg.Any<CancellationToken>())
            .Returns(projects);

        // Act
        var act = async () => await _sut.ResolveProjectIdAsync(unknownProject);

        // Assert
        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage($"Project '{unknownProject}' not found");
    }

    [Fact]
    public async Task GetCurrentUserAsync_Should_UseCache_When_CacheIsValid()
    {
        // Arrange
        var currentUser = new User { Id = 123, Name = "Test User" };
        _mockApiClient.GetCurrentUserAsync(Arg.Any<CancellationToken>())
            .Returns(currentUser);

        // Act
        var result1 = await _sut.GetCurrentUserAsync();
        var result2 = await _sut.GetCurrentUserAsync();

        // Assert
        result1.Should().BeEquivalentTo(currentUser);
        result2.Should().BeEquivalentTo(currentUser);
        await _mockApiClient.Received(1).GetCurrentUserAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetIssueStatusesAsync_Should_UseCache_When_CacheIsValid()
    {
        // Arrange
        var statuses = new List<IssueStatus>
        {
            new() { Id = 1, Name = "New" },
            new() { Id = 2, Name = "In Progress" }
        };
        _mockApiClient.GetIssueStatusesAsync(Arg.Any<CancellationToken>())
            .Returns(statuses);

        // Act
        var result1 = await _sut.GetIssueStatusesAsync();
        var result2 = await _sut.GetIssueStatusesAsync();

        // Assert
        result1.Should().BeEquivalentTo(statuses);
        result2.Should().BeEquivalentTo(statuses);
        await _mockApiClient.Received(1).GetIssueStatusesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetProjectsAsync_Should_UseCache_When_CacheIsValid()
    {
        // Arrange
        var projects = new List<Project>
        {
            new() { Id = 1, Identifier = "proj1", Name = "Project 1" },
            new() { Id = 2, Identifier = "proj2", Name = "Project 2" }
        };
        _mockApiClient.GetProjectsAsync(Arg.Any<CancellationToken>())
            .Returns(projects);

        // Act
        var result1 = await _sut.GetProjectsAsync();
        var result2 = await _sut.GetProjectsAsync();

        // Assert
        result1.Should().BeEquivalentTo(projects);
        result2.Should().BeEquivalentTo(projects);
        await _mockApiClient.Received(1).GetProjectsAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task TestConnectionAsync_Should_CallApiClient()
    {
        // Arrange
        _mockApiClient.TestConnectionAsync(Arg.Any<CancellationToken>())
            .Returns(true);

        // Act
        var result = await _sut.TestConnectionAsync();

        // Assert
        result.Should().BeTrue();
        await _mockApiClient.Received(1).TestConnectionAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task TestConnectionAsync_WithCredentials_Should_CallApiClient()
    {
        // Arrange
        const string url = "https://example.com";
        const string apiKey = "test-key";
        _mockApiClient.TestConnectionAsync(url, apiKey, Arg.Any<CancellationToken>())
            .Returns(true);

        // Act
        var result = await _sut.TestConnectionAsync(url, apiKey);

        // Assert
        result.Should().BeTrue();
        await _mockApiClient.Received(1).TestConnectionAsync(url, apiKey, Arg.Any<CancellationToken>());
    }
}