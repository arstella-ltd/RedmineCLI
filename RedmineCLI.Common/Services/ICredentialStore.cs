using RedmineCLI.Common.Models;

namespace RedmineCLI.Common.Services;

/// <summary>
/// OSのキーチェーンへのアクセスを提供するインターフェース
/// </summary>
public interface ICredentialStore : IDisposable
{
    /// <summary>
    /// 指定されたサーバーURLの認証情報を取得
    /// </summary>
    /// <param name="serverUrl">RedmineサーバーのURL</param>
    /// <returns>保存された認証情報、存在しない場合はnull</returns>
    Task<StoredCredential?> GetCredentialAsync(string serverUrl);

    /// <summary>
    /// 指定されたサーバーURLの認証情報を保存
    /// </summary>
    /// <param name="serverUrl">RedmineサーバーのURL</param>
    /// <param name="credential">保存する認証情報</param>
    Task SaveCredentialAsync(string serverUrl, StoredCredential credential);

    /// <summary>
    /// 指定されたサーバーURLの認証情報を削除
    /// </summary>
    /// <param name="serverUrl">RedmineサーバーのURL</param>
    Task DeleteCredentialAsync(string serverUrl);

    /// <summary>
    /// 指定されたサーバーURLにパスワードが保存されているか確認
    /// </summary>
    /// <param name="serverUrl">RedmineサーバーのURL</param>
    /// <returns>パスワードが保存されている場合はtrue</returns>
    Task<bool> HasStoredPasswordAsync(string serverUrl);
}
