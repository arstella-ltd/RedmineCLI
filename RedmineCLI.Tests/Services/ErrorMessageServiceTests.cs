using System.Net;
using System.Net.Sockets;

using FluentAssertions;

using RedmineCLI.Exceptions;
using RedmineCLI.Services;

using Xunit;

namespace RedmineCLI.Tests.Services;

public class ErrorMessageServiceTests
{
    private readonly ErrorMessageService _service = new();

    [Fact]
    public void GetUserFriendlyMessage_Should_Return_AuthenticationError_When_StatusCode_Is_401()
    {
        // Arrange
        var exception = new RedmineApiException(401, "Unauthorized", "Invalid API key");

        // Act
        var message = _service.GetUserFriendlyMessage(exception);

        // Assert
        message.Should().Be("認証エラー: APIキーが無効または期限切れです");
    }

    [Fact]
    public void GetUserFriendlyMessage_Should_Return_PermissionError_When_StatusCode_Is_403()
    {
        // Arrange
        var exception = new RedmineApiException(403, "Forbidden", "Access denied");

        // Act
        var message = _service.GetUserFriendlyMessage(exception);

        // Assert
        message.Should().Be("権限エラー: この操作を実行する権限がありません");
    }

    [Fact]
    public void GetUserFriendlyMessage_Should_Return_ProjectNotFound_When_404_Contains_Project()
    {
        // Arrange
        var exception = new RedmineApiException(404, "Not Found", "Project not found");

        // Act
        var message = _service.GetUserFriendlyMessage(exception);

        // Assert
        message.Should().Be("プロジェクトが見つかりません");
    }

    [Fact]
    public void GetUserFriendlyMessage_Should_Return_IssueNotFound_When_404_Contains_Issue()
    {
        // Arrange
        var exception = new RedmineApiException(404, "Not Found", "Issue not found");

        // Act
        var message = _service.GetUserFriendlyMessage(exception);

        // Assert
        message.Should().Be("チケットが見つかりません");
    }

    [Fact]
    public void GetUserFriendlyMessage_Should_Return_ValidationError()
    {
        // Arrange
        var exception = new ValidationException("Invalid input format");

        // Act
        var message = _service.GetUserFriendlyMessage(exception);

        // Assert
        message.Should().Be("入力値エラー: Invalid input format");
    }

    [Fact]
    public void GetUserFriendlyMessage_Should_Return_NetworkError_For_HttpRequestException()
    {
        // Arrange
        var exception = new HttpRequestException("Connection failed");

        // Act
        var message = _service.GetUserFriendlyMessage(exception);

        // Assert
        message.Should().Be("ネットワークエラー: サーバーに接続できません");
    }

    [Fact]
    public void GetUserFriendlyMessage_Should_Return_HostNotFound_For_SocketException()
    {
        // Arrange
        var socketException = new SocketException((int)SocketError.HostNotFound);
        var exception = new HttpRequestException("Connection failed", socketException);

        // Act
        var message = _service.GetUserFriendlyMessage(exception);

        // Assert
        message.Should().Be("ホストが見つかりません: Redmineサーバーのアドレスを確認してください");
    }

    [Fact]
    public void GetUserFriendlyMessage_Should_Return_ConnectionRefused_For_SocketException()
    {
        // Arrange
        var socketException = new SocketException((int)SocketError.ConnectionRefused);
        var exception = new HttpRequestException("Connection failed", socketException);

        // Act
        var message = _service.GetUserFriendlyMessage(exception);

        // Assert
        message.Should().Be("接続が拒否されました: Redmineサーバーが起動しているか確認してください");
    }

    [Fact]
    public void GetUserFriendlyMessage_Should_Return_TimeoutError()
    {
        // Arrange
        var exception = new TaskCanceledException();

        // Act
        var message = _service.GetUserFriendlyMessage(exception);

        // Assert
        message.Should().Be("操作がタイムアウトしました");
    }

    [Fact]
    public void GetUserFriendlyMessage_Should_Return_NoActiveProfile_Error()
    {
        // Arrange
        var exception = new InvalidOperationException("No active profile found");

        // Act
        var message = _service.GetUserFriendlyMessage(exception);

        // Assert
        message.Should().Be("アクティブなプロファイルが設定されていません");
    }

    [Fact]
    public void GetUserFriendlyMessage_Should_Return_GenericError_For_UnknownException()
    {
        // Arrange
        var exception = new ArgumentException("Some unexpected error");

        // Act
        var message = _service.GetUserFriendlyMessage(exception);

        // Assert
        message.Should().Be("エラーが発生しました: Some unexpected error");
    }

    [Fact]
    public void GetSuggestion_Should_Return_LoginSuggestion_For_401()
    {
        // Arrange
        var exception = new RedmineApiException(401, "Unauthorized", "Invalid API key");

        // Act
        var suggestion = _service.GetSuggestion(exception);

        // Assert
        suggestion.Should().Be("'redmine auth login' を実行して再度認証を行ってください");
    }

    [Fact]
    public void GetSuggestion_Should_Return_ProjectListSuggestion_For_404_Project()
    {
        // Arrange
        var exception = new RedmineApiException(404, "Not Found", "Project not found");

        // Act
        var suggestion = _service.GetSuggestion(exception);

        // Assert
        suggestion.Should().Be("利用可能なプロジェクトを確認するには 'redmine project list' を実行してください");
    }

    [Fact]
    public void GetSuggestion_Should_Return_NetworkSuggestion_For_HttpRequestException()
    {
        // Arrange
        var exception = new HttpRequestException("Connection failed");

        // Act
        var suggestion = _service.GetSuggestion(exception);

        // Assert
        suggestion.Should().Be("ネットワーク接続を確認してください。プロキシを使用している場合は、環境変数 HTTP_PROXY や HTTPS_PROXY が正しく設定されているか確認してください");
    }

    [Fact]
    public void GetSuggestion_Should_Return_LoginSuggestion_For_NoActiveProfile()
    {
        // Arrange
        var exception = new InvalidOperationException("No active profile found");

        // Act
        var suggestion = _service.GetSuggestion(exception);

        // Assert
        suggestion.Should().Be("'redmine auth login' を実行して認証を行ってください");
    }

    [Fact]
    public void GetSuggestion_Should_Return_Null_For_UnknownException()
    {
        // Arrange
        var exception = new ArgumentException("Some unexpected error");

        // Act
        var suggestion = _service.GetSuggestion(exception);

        // Assert
        suggestion.Should().BeNull();
    }
}