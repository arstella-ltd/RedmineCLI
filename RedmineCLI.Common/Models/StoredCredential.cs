namespace RedmineCLI.Common.Models;

/// <summary>
/// OSキーチェーンに保存される認証情報
/// </summary>
public class StoredCredential
{
    /// <summary>
    /// ユーザー名
    /// </summary>
    public string? Username { get; set; }

    /// <summary>
    /// パスワード
    /// </summary>
    public string? Password { get; set; }

    /// <summary>
    /// APIキー
    /// </summary>
    public string? ApiKey { get; set; }

    /// <summary>
    /// セッションクッキー
    /// </summary>
    public string? SessionCookie { get; set; }

    /// <summary>
    /// セッションの有効期限
    /// </summary>
    public DateTime? SessionExpiry { get; set; }

    /// <summary>
    /// 最終更新日時
    /// </summary>
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 有効なセッションを持っているか確認
    /// </summary>
    public bool HasValidSession()
    {
        return !string.IsNullOrEmpty(SessionCookie)
            && SessionExpiry.HasValue
            && SessionExpiry.Value > DateTime.UtcNow;
    }

    /// <summary>
    /// パスワード認証情報を持っているか確認
    /// </summary>
    public bool HasPasswordCredentials()
    {
        return !string.IsNullOrEmpty(Username) && !string.IsNullOrEmpty(Password);
    }
}
