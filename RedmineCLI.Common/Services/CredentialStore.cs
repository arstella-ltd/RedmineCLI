using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

using RedmineCLI.Common.Models;

namespace RedmineCLI.Common.Services;

/// <summary>
/// 認証情報ストアの基底クラスとファクトリ
/// </summary>
public abstract class CredentialStore : ICredentialStore
{
    /// <summary>
    /// プラットフォームに応じた適切なCredentialStore実装を作成
    /// </summary>
    public static ICredentialStore Create()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return new WindowsCredentialStore();
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            return new MacOSCredentialStore();
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            // Linux環境でもsecret-toolが利用できない場合はファイルベースにフォールバック
            if (LinuxCredentialStore.IsSecretToolAvailable())
            {
                return new LinuxCredentialStore();
            }
            else
            {
                return new FileBasedCredentialStore();
            }
        }
        else
        {
            // フォールバック: ファイルベースの実装
            return new FileBasedCredentialStore();
        }
    }

    /// <summary>
    /// サーバーURLからキー名を生成
    /// </summary>
    protected static string GetKeyName(string serverUrl)
    {
        // URLを正規化してキー名として使用
        var uri = new Uri(serverUrl);
        return $"RedmineCLI:{uri.Host}:{uri.Port}";
    }

    // ICredentialStoreの実装
    [RequiresDynamicCode("JSON serialization may require dynamic code generation")]
    [RequiresUnreferencedCode("JSON serialization may require unreferenced code")]
    public abstract Task<StoredCredential?> GetCredentialAsync(string serverUrl);

    [RequiresDynamicCode("JSON serialization may require dynamic code generation")]
    [RequiresUnreferencedCode("JSON serialization may require unreferenced code")]
    public abstract Task SaveCredentialAsync(string serverUrl, StoredCredential credential);

    public abstract Task DeleteCredentialAsync(string serverUrl);

    [RequiresUnreferencedCode("JSON serialization may require unreferenced code")]
    [RequiresDynamicCode("JSON serialization may require dynamic code generation")]
    public virtual async Task<bool> HasStoredPasswordAsync(string serverUrl)
    {
        var credential = await GetCredentialAsync(serverUrl);
        return credential?.Password != null;
    }

    // IDisposableの実装
    protected bool disposed = false;

    protected virtual void Dispose(bool disposing)
    {
        if (!disposed)
        {
            if (disposing)
            {
                // マネージドリソースの解放
            }
            disposed = true;
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}
