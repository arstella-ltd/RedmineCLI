using System.Net;

using RedmineCLI.Exceptions;

namespace RedmineCLI.Services;

public class ErrorMessageService : IErrorMessageService
{
    public string GetUserFriendlyMessage(Exception exception)
    {
        return exception switch
        {
            RedmineApiException apiEx => GetApiErrorMessage(apiEx),
            ValidationException valEx => $"入力値エラー: {valEx.Message}",
            HttpRequestException httpEx => GetNetworkErrorMessage(httpEx),
            TaskCanceledException => "操作がタイムアウトしました",
            UnauthorizedAccessException => "ファイルまたはディレクトリへのアクセスが拒否されました",
            DirectoryNotFoundException dirEx => $"ディレクトリが見つかりません: {dirEx.Message}",
            FileNotFoundException fileEx => $"ファイルが見つかりません: {fileEx.Message}",
            InvalidOperationException invEx when invEx.Message.Contains("No active profile") =>
                "アクティブなプロファイルが設定されていません",
            InvalidOperationException invEx when invEx.Message.Contains("No API key found") =>
                "APIキーが設定されていません",
            _ => $"エラーが発生しました: {exception.Message}"
        };
    }

    public string? GetSuggestion(Exception exception)
    {
        return exception switch
        {
            RedmineApiException apiEx => GetApiErrorSuggestion(apiEx),
            ValidationException => "入力値を確認して再度お試しください",
            HttpRequestException => "ネットワーク接続を確認してください。プロキシを使用している場合は、環境変数 HTTP_PROXY や HTTPS_PROXY が正しく設定されているか確認してください",
            TaskCanceledException => "タイムアウト時間を延長するか、ネットワーク接続を確認してください",
            UnauthorizedAccessException => "ファイルやディレクトリのアクセス権限を確認してください",
            InvalidOperationException invEx when invEx.Message.Contains("No active profile") =>
                "'redmine auth login' を実行して認証を行ってください",
            InvalidOperationException invEx when invEx.Message.Contains("No API key found") =>
                "'redmine auth login' を実行して認証を行ってください",
            _ => null
        };
    }

    private static string GetApiErrorMessage(RedmineApiException exception)
    {
        return exception.StatusCode switch
        {
            401 => "認証エラー: APIキーが無効または期限切れです",
            403 => "権限エラー: この操作を実行する権限がありません",
            404 => GetNotFoundMessage(exception),
            422 => "入力エラー: 送信されたデータが無効です",
            500 => "サーバーエラー: Redmineサーバーで問題が発生しました",
            502 => "ゲートウェイエラー: Redmineサーバーに接続できません",
            503 => "サービス利用不可: Redmineサーバーが一時的に利用できません",
            _ => $"APIエラー ({exception.StatusCode}): {exception.Message}"
        };
    }

    private static string GetNotFoundMessage(RedmineApiException exception)
    {
        var message = (exception.ApiError ?? exception.Message).ToLower();
        
        if (message.Contains("project"))
        {
            return "プロジェクトが見つかりません";
        }
        else if (message.Contains("issue"))
        {
            return "チケットが見つかりません";
        }
        else if (message.Contains("user"))
        {
            return "ユーザーが見つかりません";
        }
        else if (message.Contains("attachment"))
        {
            return "添付ファイルが見つかりません";
        }
        
        return "リソースが見つかりません";
    }

    private static string? GetApiErrorSuggestion(RedmineApiException exception)
    {
        return exception.StatusCode switch
        {
            401 => "'redmine auth login' を実行して再度認証を行ってください",
            403 => "アクセス権限を確認するか、管理者に問い合わせてください",
            404 => GetNotFoundSuggestion(exception),
            422 => "入力内容を確認して再度お試しください",
            500 => "しばらく待ってから再度お試しください",
            502 => "Redmineサーバーのアドレスが正しいか確認してください",
            503 => "しばらく待ってから再度お試しください",
            _ => null
        };
    }

    private static string? GetNotFoundSuggestion(RedmineApiException exception)
    {
        var message = (exception.ApiError ?? exception.Message).ToLower();
        
        if (message.Contains("project"))
        {
            return "利用可能なプロジェクトを確認するには 'redmine project list' を実行してください";
        }
        else if (message.Contains("issue"))
        {
            return "チケットIDが正しいか確認してください。チケット一覧を表示するには 'redmine issue list' を実行してください";
        }
        else if (message.Contains("user"))
        {
            return "ユーザー名またはユーザーIDが正しいか確認してください";
        }
        
        return "リソースの識別子やIDが正しいか確認してください";
    }

    private static string GetNetworkErrorMessage(HttpRequestException exception)
    {
        if (exception.InnerException is System.Net.Sockets.SocketException socketEx)
        {
            return socketEx.SocketErrorCode switch
            {
                System.Net.Sockets.SocketError.HostNotFound => "ホストが見つかりません: Redmineサーバーのアドレスを確認してください",
                System.Net.Sockets.SocketError.ConnectionRefused => "接続が拒否されました: Redmineサーバーが起動しているか確認してください",
                System.Net.Sockets.SocketError.TimedOut => "接続がタイムアウトしました",
                _ => "ネットワークエラー: サーバーに接続できません"
            };
        }
        
        return "ネットワークエラー: サーバーに接続できません";
    }
}