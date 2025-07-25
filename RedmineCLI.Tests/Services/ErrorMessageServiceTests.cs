using System.Net;
using FluentAssertions;
using RedmineCLI.Exceptions;
using RedmineCLI.Services;
using Xunit;

namespace RedmineCLI.Tests.Services;

[Trait("Category", "Unit")]
public class ErrorMessageServiceTests
{
    private readonly ErrorMessageService _service;

    public ErrorMessageServiceTests()
    {
        _service = new ErrorMessageService();
    }

    [Fact]
    public void GetUserFriendlyMessage_Should_ReturnAuthenticationError_When_RedmineApiExceptionWith401()
    {
        // Arrange
        var exception = new RedmineApiException((int)HttpStatusCode.Unauthorized, "Unauthorized");

        // Act
        var (message, suggestion) = _service.GetUserFriendlyMessage(exception);

        // Assert
        message.Should().Be("Authentication failed");
        suggestion.Should().Be("Run 'redmine auth login' to authenticate again");
    }

    [Fact]
    public void GetUserFriendlyMessage_Should_ReturnAccessDeniedError_When_RedmineApiExceptionWith403()
    {
        // Arrange
        var exception = new RedmineApiException((int)HttpStatusCode.Forbidden, "Forbidden");

        // Act
        var (message, suggestion) = _service.GetUserFriendlyMessage(exception);

        // Assert
        message.Should().Be("Access denied");
        suggestion.Should().Be("Check your API key permissions");
    }

    [Fact]
    public void GetUserFriendlyMessage_Should_ReturnProjectNotFoundError_When_RedmineApiExceptionWith404AndProjectInMessage()
    {
        // Arrange
        var exception = new RedmineApiException((int)HttpStatusCode.NotFound, "Project not found");

        // Act
        var (message, suggestion) = _service.GetUserFriendlyMessage(exception);

        // Assert
        message.Should().Be("Project not found");
        suggestion.Should().Contain("redmine project list");
    }

    [Fact]
    public void GetUserFriendlyMessage_Should_ReturnIssueNotFoundError_When_RedmineApiExceptionWith404AndIssueInMessage()
    {
        // Arrange
        var exception = new RedmineApiException((int)HttpStatusCode.NotFound, "Issue not found");

        // Act
        var (message, suggestion) = _service.GetUserFriendlyMessage(exception);

        // Assert
        message.Should().Be("Issue not found");
        suggestion.Should().Be("Check the issue ID");
    }

    [Fact]
    public void GetUserFriendlyMessage_Should_ReturnRateLimitError_When_RedmineApiExceptionWith429()
    {
        // Arrange
        var exception = new RedmineApiException((int)HttpStatusCode.TooManyRequests, "Too Many Requests");

        // Act
        var (message, suggestion) = _service.GetUserFriendlyMessage(exception);

        // Assert
        message.Should().Be("API rate limit exceeded");
        suggestion.Should().Be("Wait a while and try again");
    }

    [Fact]
    public void GetUserFriendlyMessage_Should_ReturnNetworkError_When_HttpRequestExceptionWithSocketException()
    {
        // Arrange
        var innerException = new System.Net.Sockets.SocketException();
        var exception = new HttpRequestException("Connection failed", innerException);

        // Act
        var (message, suggestion) = _service.GetUserFriendlyMessage(exception);

        // Assert
        message.Should().Be("Cannot connect to server");
        suggestion.Should().Be("Check your network connection and Redmine URL");
    }

    [Fact]
    public void GetUserFriendlyMessage_Should_ReturnSSLError_When_HttpRequestExceptionWithSSLInMessage()
    {
        // Arrange
        var exception = new HttpRequestException("SSL connection error");

        // Act
        var (message, suggestion) = _service.GetUserFriendlyMessage(exception);

        // Assert
        message.Should().Be("SSL/TLS connection error occurred");
        suggestion.Should().Be("Check your certificate settings");
    }

    [Fact]
    public void GetUserFriendlyMessage_Should_ReturnApiKeyNotSetError_When_InvalidOperationExceptionWithApiKeyInMessage()
    {
        // Arrange
        var exception = new InvalidOperationException("No API key found");

        // Act
        var (message, suggestion) = _service.GetUserFriendlyMessage(exception);

        // Assert
        message.Should().Be("API key is not configured");
        suggestion.Should().Be("Run 'redmine auth login' to authenticate");
    }

    [Fact]
    public void GetUserFriendlyMessage_Should_ReturnNoActiveProfileError_When_InvalidOperationExceptionWithProfileInMessage()
    {
        // Arrange
        var exception = new InvalidOperationException("No active profile configured");

        // Act
        var (message, suggestion) = _service.GetUserFriendlyMessage(exception);

        // Assert
        message.Should().Be("No active profile is configured");
        suggestion.Should().Be("Run 'redmine config set active_profile <profile-name>' to set a profile");
    }

    [Fact]
    public void GetUserFriendlyMessage_Should_ReturnTimeoutError_When_TaskCanceledException()
    {
        // Arrange
        var exception = new TaskCanceledException();

        // Act
        var (message, suggestion) = _service.GetUserFriendlyMessage(exception);

        // Assert
        message.Should().Be("Request timed out");
        suggestion.Should().Be("Check your network connection and try again");
    }

    [Fact]
    public void GetUserFriendlyMessage_Should_ReturnOriginalMessage_When_UnknownException()
    {
        // Arrange
        var exception = new NotSupportedException("This operation is not supported");

        // Act
        var (message, suggestion) = _service.GetUserFriendlyMessage(exception);

        // Assert
        message.Should().Be("This operation is not supported");
        suggestion.Should().BeNull();
    }
}