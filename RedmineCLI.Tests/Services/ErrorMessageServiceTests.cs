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
        message.Should().Be("認証に失敗しました");
        suggestion.Should().Be("'redmine auth login' を実行して再度認証を行ってください");
    }

    [Fact]
    public void GetUserFriendlyMessage_Should_ReturnAccessDeniedError_When_RedmineApiExceptionWith403()
    {
        // Arrange
        var exception = new RedmineApiException((int)HttpStatusCode.Forbidden, "Forbidden");

        // Act
        var (message, suggestion) = _service.GetUserFriendlyMessage(exception);

        // Assert
        message.Should().Be("アクセスが拒否されました");
        suggestion.Should().Be("APIキーの権限を確認してください");
    }

    [Fact]
    public void GetUserFriendlyMessage_Should_ReturnProjectNotFoundError_When_RedmineApiExceptionWith404AndProjectInMessage()
    {
        // Arrange
        var exception = new RedmineApiException((int)HttpStatusCode.NotFound, "Project not found");

        // Act
        var (message, suggestion) = _service.GetUserFriendlyMessage(exception);

        // Assert
        message.Should().Be("プロジェクトが見つかりません");
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
        message.Should().Be("チケットが見つかりません");
        suggestion.Should().Be("チケットIDを確認してください");
    }

    [Fact]
    public void GetUserFriendlyMessage_Should_ReturnRateLimitError_When_RedmineApiExceptionWith429()
    {
        // Arrange
        var exception = new RedmineApiException((int)HttpStatusCode.TooManyRequests, "Too Many Requests");

        // Act
        var (message, suggestion) = _service.GetUserFriendlyMessage(exception);

        // Assert
        message.Should().Be("APIのレート制限に達しました");
        suggestion.Should().Be("しばらく待ってから再度お試しください");
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
        message.Should().Be("サーバーに接続できません");
        suggestion.Should().Be("ネットワーク接続とRedmineのURLを確認してください");
    }

    [Fact]
    public void GetUserFriendlyMessage_Should_ReturnSSLError_When_HttpRequestExceptionWithSSLInMessage()
    {
        // Arrange
        var exception = new HttpRequestException("SSL connection error");

        // Act
        var (message, suggestion) = _service.GetUserFriendlyMessage(exception);

        // Assert
        message.Should().Be("SSL/TLS接続エラーが発生しました");
        suggestion.Should().Be("証明書の設定を確認してください");
    }

    [Fact]
    public void GetUserFriendlyMessage_Should_ReturnApiKeyNotSetError_When_InvalidOperationExceptionWithApiKeyInMessage()
    {
        // Arrange
        var exception = new InvalidOperationException("No API key found");

        // Act
        var (message, suggestion) = _service.GetUserFriendlyMessage(exception);

        // Assert
        message.Should().Be("APIキーが設定されていません");
        suggestion.Should().Be("'redmine auth login' を実行して認証を行ってください");
    }

    [Fact]
    public void GetUserFriendlyMessage_Should_ReturnNoActiveProfileError_When_InvalidOperationExceptionWithProfileInMessage()
    {
        // Arrange
        var exception = new InvalidOperationException("No active profile configured");

        // Act
        var (message, suggestion) = _service.GetUserFriendlyMessage(exception);

        // Assert
        message.Should().Be("アクティブなプロファイルが設定されていません");
        suggestion.Should().Be("'redmine config set active_profile <profile-name>' を実行してプロファイルを設定してください");
    }

    [Fact]
    public void GetUserFriendlyMessage_Should_ReturnTimeoutError_When_TaskCanceledException()
    {
        // Arrange
        var exception = new TaskCanceledException();

        // Act
        var (message, suggestion) = _service.GetUserFriendlyMessage(exception);

        // Assert
        message.Should().Be("リクエストがタイムアウトしました");
        suggestion.Should().Be("ネットワーク接続を確認して、再度お試しください");
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