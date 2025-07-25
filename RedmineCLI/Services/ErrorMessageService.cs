using System.Net;
using RedmineCLI.Exceptions;

namespace RedmineCLI.Services;

public interface IErrorMessageService
{
    (string message, string? suggestion) GetUserFriendlyMessage(Exception exception);
}

public class ErrorMessageService : IErrorMessageService
{
    public (string message, string? suggestion) GetUserFriendlyMessage(Exception exception)
    {
        return exception switch
        {
            RedmineApiException apiEx => GetApiErrorMessage(apiEx),
            HttpRequestException httpEx => GetHttpErrorMessage(httpEx),
            InvalidOperationException invOpEx when invOpEx.Message.Contains("API key") => 
                ("APIキーが設定されていません", "'redmine auth login' を実行して認証を行ってください"),
            InvalidOperationException invOpEx when invOpEx.Message.Contains("No active profile") => 
                ("アクティブなプロファイルが設定されていません", "'redmine config set active_profile <profile-name>' を実行してプロファイルを設定してください"),
            ArgumentException argEx when argEx.Message.Contains("Project ID") =>
                ("無効なプロジェクトIDが指定されました", "'redmine project list' を実行して利用可能なプロジェクトを確認してください"),
            TaskCanceledException =>
                ("リクエストがタイムアウトしました", "ネットワーク接続を確認して、再度お試しください"),
            _ => (exception.Message, null)
        };
    }

    private (string message, string? suggestion) GetApiErrorMessage(RedmineApiException exception)
    {
        return exception.StatusCode switch
        {
            (int)HttpStatusCode.Unauthorized => 
                ("認証に失敗しました", "'redmine auth login' を実行して再度認証を行ってください"),
            (int)HttpStatusCode.Forbidden => 
                ("アクセスが拒否されました", "APIキーの権限を確認してください"),
            (int)HttpStatusCode.NotFound when exception.Message.Contains("project", StringComparison.OrdinalIgnoreCase) =>
                ("プロジェクトが見つかりません", $"指定されたプロジェクトは存在しません。\n利用可能なプロジェクトを確認するには 'redmine project list' を実行してください"),
            (int)HttpStatusCode.NotFound when exception.Message.Contains("issue", StringComparison.OrdinalIgnoreCase) =>
                ("チケットが見つかりません", "チケットIDを確認してください"),
            (int)HttpStatusCode.UnprocessableEntity =>
                ("無効なデータが送信されました", "入力内容を確認してください"),
            (int)HttpStatusCode.TooManyRequests =>
                ("APIのレート制限に達しました", "しばらく待ってから再度お試しください"),
            _ => ($"APIエラー: {exception.Message}", exception.ApiError)
        };
    }

    private (string message, string? suggestion) GetHttpErrorMessage(HttpRequestException exception)
    {
        if (exception.InnerException is System.Net.Sockets.SocketException)
        {
            return ("サーバーに接続できません", "ネットワーク接続とRedmineのURLを確認してください");
        }

        if (exception.Message.Contains("SSL", StringComparison.OrdinalIgnoreCase) || 
            exception.Message.Contains("HTTPS", StringComparison.OrdinalIgnoreCase))
        {
            return ("SSL/TLS接続エラーが発生しました", "証明書の設定を確認してください");
        }

        return ("ネットワークエラーが発生しました", "ネットワーク接続を確認してください");
    }
}