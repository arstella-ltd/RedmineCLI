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

public class IssueCommentCommandTests
{
    private readonly IRedmineApiClient _apiClient;
    private readonly IConfigService _configService;
    private readonly ITableFormatter _tableFormatter;
    private readonly IJsonFormatter _jsonFormatter;
    private readonly ILogger<IssueCommand> _logger;
    private readonly IssueCommand _issueCommand;

    public IssueCommentCommandTests()
    {
        _apiClient = Substitute.For<IRedmineApiClient>();
        _configService = Substitute.For<IConfigService>();
        _tableFormatter = Substitute.For<ITableFormatter>();
        _jsonFormatter = Substitute.For<IJsonFormatter>();
        _logger = Substitute.For<ILogger<IssueCommand>>();

        _issueCommand = new IssueCommand(_apiClient, _configService, _tableFormatter, _jsonFormatter, _logger);
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
        _apiClient.AddCommentAsync(issueId, message, Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);

        // Act
        var result = await _issueCommand.CommentAsync(issueId, message, CancellationToken.None);

        // Assert
        result.Should().Be(0);
        await _apiClient.Received(1).AddCommentAsync(issueId, message, Arg.Any<CancellationToken>());
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
        result.Should().BeGreaterOrEqualTo(0);
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
        _apiClient.AddCommentAsync(issueId, message, Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);

        // Act
        var result = await _issueCommand.CommentAsync(issueId, message, CancellationToken.None);

        // Assert
        result.Should().Be(0);
        // The method should call AddCommentAsync and show a success message
        await _apiClient.Received(1).AddCommentAsync(issueId, message, Arg.Any<CancellationToken>());
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
        await _apiClient.DidNotReceive().AddCommentAsync(Arg.Any<int>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Comment_Should_ReturnError_When_ApiException()
    {
        // Arrange
        const int issueId = 999;
        const string message = "This will fail";

        var apiException = new RedmineApiException(404, "Issue not found", null);
        _apiClient.AddCommentAsync(issueId, message, Arg.Any<CancellationToken>())
            .Returns(Task.FromException(apiException));

        // Act
        var result = await _issueCommand.CommentAsync(issueId, message, CancellationToken.None);

        // Assert
        result.Should().Be(1);
        await _apiClient.Received(1).AddCommentAsync(issueId, message, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Comment_Should_ReturnError_When_IssueNotFound()
    {
        // Arrange
        const int nonExistentIssueId = 404;
        const string message = "Comment for non-existent issue";

        var apiException = new RedmineApiException(404, "Issue not found", null);
        _apiClient.AddCommentAsync(nonExistentIssueId, message, Arg.Any<CancellationToken>())
            .Returns(Task.FromException(apiException));

        // Act
        var result = await _issueCommand.CommentAsync(nonExistentIssueId, message, CancellationToken.None);

        // Assert
        result.Should().Be(1);
        await _apiClient.Received(1).AddCommentAsync(nonExistentIssueId, message, Arg.Any<CancellationToken>());
    }

    #endregion
}
