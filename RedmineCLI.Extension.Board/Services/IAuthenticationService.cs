using RedmineCLI.Common.Models;

namespace RedmineCLI.Extension.Board.Services;

/// <summary>
/// 認証とセッション管理のためのサービスインターフェース
/// </summary>
public interface IAuthenticationService
{
    /// <summary>
    /// 認証情報を取得する
    /// </summary>
    /// <param name="urlOverride">URLオーバーライド（オプション）</param>
    /// <returns>RedmineのURLとセッションクッキー</returns>
    Task<(string url, string? sessionCookie)> GetAuthenticationAsync(string? urlOverride);
}
