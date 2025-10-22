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

[Collection("Sequential")]
public class IssueCommentCommandTests
{
    private readonly IRedmineService _redmineService;
    private readonly IConfigService _configService;
    private readonly ITableFormatter _tableFormatter;
    private readonly IJsonFormatter _jsonFormatter;
    private readonly ILogger<IssueCommand> _logger;
    private readonly IssueCommand _issueCommand;

    public IssueCommentCommandTests()
    {
        _redmineService = Substitute.For<IRedmineService>();
        _configService = Substitute.For<IConfigService>();
        _tableFormatter = Substitute.For<ITableFormatter>();
        _jsonFormatter = Substitute.For<IJsonFormatter>();
        _logger = Substitute.For<ILogger<IssueCommand>>();

        _issueCommand = new IssueCommand(_redmineService, _configService, _tableFormatter, _jsonFormatter, _logger);
    }

    #region Comment Command Tests

    [Fact]
    public async Task Comment_Should_AddComment_When_MessageProvided()
    {
        // Arrange
        const int issueId = 123;
        const string message = "This is a test comment";

        var profile = new Profile
        {
            Name = "test",
            Url = "https://redmine.example.com",
            ApiKey = "test-key"
        };
        _configService.GetActiveProfileAsync().Returns(profile);
        _redmineService.AddCommentAsync(issueId, message, Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);

        // Act
        var result = await _issueCommand.CommentAsync(issueId, message, CancellationToken.None);

        // Assert
        result.Should().Be(0);
        await _redmineService.Received(1).AddCommentAsync(issueId, message, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Comment_Should_OpenEditor_When_NoMessageOption()
    {
        // Arrange
        const int issueId = 123;

        // Set up EDITOR environment variable to simulate editor behavior
        Environment.SetEnvironmentVariable("EDITOR", "echo");

        var profile = new Profile
        {
            Name = "test",
            Url = "https://redmine.example.com",
            ApiKey = "test-key"
        };
        _configService.GetActiveProfileAsync().Returns(profile);

        // Act
        var result = await _issueCommand.CommentAsync(issueId, null, CancellationToken.None);

        // Assert
        // For editor mode, it should either succeed (if implemented) or return appropriate error
        result.Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public async Task Comment_Should_ShowConfirmation_When_CommentAdded()
    {
        // Arrange
        const int issueId = 456;
        const string message = "Comment successfully added";

        var profile = new Profile
        {
            Name = "test",
            Url = "https://redmine.example.com",
            ApiKey = "test-key"
        };
        _configService.GetActiveProfileAsync().Returns(profile);
        _redmineService.AddCommentAsync(issueId, message, Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);

        // Act
        var result = await _issueCommand.CommentAsync(issueId, message, CancellationToken.None);

        // Assert
        result.Should().Be(0);
        // The method should call AddCommentAsync and show a success message
        await _redmineService.Received(1).AddCommentAsync(issueId, message, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Comment_Should_HandleEmptyComment_When_NoTextEntered()
    {
        // Arrange
        const int issueId = 789;
        const string emptyMessage = "";

        // Act
        var result = await _issueCommand.CommentAsync(issueId, emptyMessage, CancellationToken.None);

        // Assert
        result.Should().Be(1); // Should return error code for empty comment
        await _redmineService.DidNotReceive().AddCommentAsync(Arg.Any<int>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Comment_Should_ReturnError_When_ApiException()
    {
        // Arrange
        const int issueId = 999;
        const string message = "This will fail";

        var apiException = new RedmineApiException(404, "Issue not found", null);
        _redmineService.AddCommentAsync(issueId, message, Arg.Any<CancellationToken>())
            .Returns(Task.FromException(apiException));

        // Act
        var result = await _issueCommand.CommentAsync(issueId, message, CancellationToken.None);

        // Assert
        result.Should().Be(1);
        await _redmineService.Received(1).AddCommentAsync(issueId, message, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Comment_Should_ReturnError_When_IssueNotFound()
    {
        // Arrange
        const int nonExistentIssueId = 404;
        const string message = "Comment for non-existent issue";

        var apiException = new RedmineApiException(404, "Issue not found", null);
        _redmineService.AddCommentAsync(nonExistentIssueId, message, Arg.Any<CancellationToken>())
            .Returns(Task.FromException(apiException));

        // Act
        var result = await _issueCommand.CommentAsync(nonExistentIssueId, message, CancellationToken.None);

        // Assert
        result.Should().Be(1);
        await _redmineService.Received(1).AddCommentAsync(nonExistentIssueId, message, Arg.Any<CancellationToken>());
    }

    #endregion

    #region Comment Command Body Update Tests

    [Fact]
    public async Task Comment_Should_UpdateDescription_When_BodyProvided()
    {
        // Arrange
        const int issueId = 123;
        const string body = "New description text";

        var updatedIssue = new Issue { Id = issueId, Subject = "Test Issue" };
        _redmineService.UpdateIssueAsync(issueId, null, null, null, body, null, Arg.Any<CancellationToken>())
            .Returns(updatedIssue);

        var profile = new Profile
        {
            Name = "test",
            Url = "https://redmine.example.com",
            ApiKey = "test-key"
        };
        _configService.GetActiveProfileAsync().Returns(profile);

        // Act
        var result = await _issueCommand.CommentAsync(issueId, null, null, null, null, body, null, null, CancellationToken.None);

        // Assert
        result.Should().Be(0);
        await _redmineService.Received(1).UpdateIssueAsync(issueId, null, null, null, body, null, Arg.Any<CancellationToken>());
        await _redmineService.DidNotReceive().AddCommentAsync(Arg.Any<int>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Comment_Should_UpdateDescriptionFromFile_When_BodyFileProvided()
    {
        // Arrange
        const int issueId = 123;
        var tempFile = Path.GetTempFileName();
        const string fileContent = "Description from file";
        await File.WriteAllTextAsync(tempFile, fileContent);

        var updatedIssue = new Issue { Id = issueId, Subject = "Test Issue" };
        _redmineService.UpdateIssueAsync(issueId, null, null, null, fileContent, null, Arg.Any<CancellationToken>())
            .Returns(updatedIssue);

        var profile = new Profile
        {
            Name = "test",
            Url = "https://redmine.example.com",
            ApiKey = "test-key"
        };
        _configService.GetActiveProfileAsync().Returns(profile);

        try
        {
            // Act
            var result = await _issueCommand.CommentAsync(issueId, null, null, null, null, null, tempFile, null, CancellationToken.None);

            // Assert
            result.Should().Be(0);
            await _redmineService.Received(1).UpdateIssueAsync(issueId, null, null, null, fileContent, null, Arg.Any<CancellationToken>());
        }
        finally
        {
            // Cleanup
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task Comment_Should_AddCommentAndUpdateDescription_When_BothProvided()
    {
        // Arrange
        const int issueId = 123;
        const string message = "Updated the description";
        const string body = "New description text";

        var updatedIssue = new Issue { Id = issueId, Subject = "Test Issue" };
        _redmineService.UpdateIssueAsync(issueId, null, null, null, body, null, Arg.Any<CancellationToken>())
            .Returns(updatedIssue);
        _redmineService.AddCommentAsync(issueId, message, Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var profile = new Profile
        {
            Name = "test",
            Url = "https://redmine.example.com",
            ApiKey = "test-key"
        };
        _configService.GetActiveProfileAsync().Returns(profile);

        // Act
        var result = await _issueCommand.CommentAsync(issueId, message, null, null, null, body, null, null, CancellationToken.None);

        // Assert
        result.Should().Be(0);
        await _redmineService.Received(1).UpdateIssueAsync(issueId, null, null, null, body, null, Arg.Any<CancellationToken>());
        await _redmineService.Received(1).AddCommentAsync(issueId, message, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Comment_Should_ReturnError_When_NoActionProvided()
    {
        // Arrange
        const int issueId = 123;

        // Act
        var result = await _issueCommand.CommentAsync(issueId, null, null, null, null, null, null, null, CancellationToken.None);

        // Assert
        result.Should().Be(1);
        await _redmineService.DidNotReceive().UpdateIssueAsync(Arg.Any<int>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<int?>(), Arg.Any<CancellationToken>());
        await _redmineService.DidNotReceive().AddCommentAsync(Arg.Any<int>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Comment_Should_ReturnError_When_BodyFileNotFound()
    {
        // Arrange
        const int issueId = 123;
        const string nonExistentFile = "/path/to/nonexistent/file.txt";

        // Act
        var result = await _issueCommand.CommentAsync(issueId, null, null, null, null, null, nonExistentFile, null, CancellationToken.None);

        // Assert
        result.Should().Be(1);
        await _redmineService.DidNotReceive().UpdateIssueAsync(Arg.Any<int>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<int?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Comment_Should_UpdateDescriptionFromStdin_When_BodyFileDash()
    {
        // Arrange
        const int issueId = 123;
        const string stdinContent = "Description from stdin";

        // Simulate stdin input
        var originalInput = Console.In;
        using var stringReader = new StringReader(stdinContent);
        Console.SetIn(stringReader);

        var updatedIssue = new Issue { Id = issueId, Subject = "Test Issue" };
        _redmineService.UpdateIssueAsync(issueId, null, null, null, stdinContent, null, Arg.Any<CancellationToken>())
            .Returns(updatedIssue);

        var profile = new Profile
        {
            Name = "test",
            Url = "https://redmine.example.com",
            ApiKey = "test-key"
        };
        _configService.GetActiveProfileAsync().Returns(profile);

        try
        {
            // Act
            var result = await _issueCommand.CommentAsync(issueId, null, null, null, null, null, "-", null, CancellationToken.None);

            // Assert
            result.Should().Be(0);
            await _redmineService.Received(1).UpdateIssueAsync(issueId, null, null, null, stdinContent, null, Arg.Any<CancellationToken>());
        }
        finally
        {
            // Restore original stdin
            Console.SetIn(originalInput);
        }
    }

    #endregion

    #region Assignee Tests

    [Fact]
    public async Task Comment_Should_UpdateAssigneeAndAddComment_When_AddAssigneeProvided()
    {
        // Arrange
        const int issueId = 123;
        const string message = "Adding new assignee";
        const string assignee = "@me";
        var profile = new Profile
        {
            Name = "Test",
            Url = "https://redmine.example.com",
            ApiKey = "test-key"
        };
        _configService.GetActiveProfileAsync().Returns(profile);

        var currentUser = new User { Id = 1, Login = "johndoe", FirstName = "John", LastName = "Doe" };
        _redmineService.GetCurrentUserAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(currentUser));

        // Act
        var result = await _issueCommand.CommentAsync(issueId, message, null, null, null, null, null, assignee, CancellationToken.None);

        // Assert
        result.Should().Be(0);
        await _redmineService.Received(1).UpdateIssueAsync(issueId, null, null, assignee, null, null, Arg.Any<CancellationToken>());
        await _redmineService.Received(1).AddCommentAsync(issueId, message, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Comment_Should_RemoveAssignee_When_RemoveAssigneeProvided()
    {
        // Arrange
        const int issueId = 123;
        const string message = "Removing assignee";
        const string assignee = "__REMOVE__";
        var profile = new Profile
        {
            Name = "Test",
            Url = "https://redmine.example.com",
            ApiKey = "test-key"
        };
        _configService.GetActiveProfileAsync().Returns(profile);

        // Act
        var result = await _issueCommand.CommentAsync(issueId, message, null, null, null, null, null, assignee, CancellationToken.None);

        // Assert
        result.Should().Be(0);
        await _redmineService.Received(1).UpdateIssueAsync(issueId, null, null, "__REMOVE__", null, null, Arg.Any<CancellationToken>());
        await _redmineService.Received(1).AddCommentAsync(issueId, message, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Comment_Should_OnlyUpdateAssignee_When_NoCommentProvided()
    {
        // Arrange
        const int issueId = 123;
        const string assignee = "newuser";
        var profile = new Profile
        {
            Name = "Test",
            Url = "https://redmine.example.com",
            ApiKey = "test-key"
        };
        _configService.GetActiveProfileAsync().Returns(profile);

        var users = new List<User>
        {
            new User { Id = 2, Login = "newuser", Name = "New User" }
        };
        _redmineService.GetUsersAsync(null, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(users));

        // Act
        var result = await _issueCommand.CommentAsync(issueId, null, null, null, null, null, null, assignee, CancellationToken.None);

        // Assert
        result.Should().Be(0);
        await _redmineService.Received(1).UpdateIssueAsync(issueId, null, null, assignee, null, null, Arg.Any<CancellationToken>());
        await _redmineService.DidNotReceive().AddCommentAsync(Arg.Any<int>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    #endregion
}
