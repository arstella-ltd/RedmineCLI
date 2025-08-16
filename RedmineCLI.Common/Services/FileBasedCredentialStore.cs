using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.Json;

using RedmineCLI.Common.Models;

namespace RedmineCLI.Common.Services;

/// <summary>
/// ファイルベースの認証情報ストア（フォールバック実装）
/// </summary>
public class FileBasedCredentialStore : CredentialStore
{
    private readonly string _credentialsDirectory;

    public FileBasedCredentialStore()
    {
        // 設定ディレクトリの決定
        var homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        _credentialsDirectory = Path.Combine(homeDir, ".config", "redmine", "credentials");
        Directory.CreateDirectory(_credentialsDirectory);
    }

    [RequiresDynamicCode("JSON serialization may require dynamic code generation")]
    [RequiresUnreferencedCode("JSON serialization may require unreferenced code")]
    public override async Task<StoredCredential?> GetCredentialAsync(string serverUrl)
    {
        var filePath = GetCredentialFilePath(serverUrl);

        if (!File.Exists(filePath))
            return null;

        try
        {
            var encryptedJson = await File.ReadAllTextAsync(filePath);
            var json = DecryptString(encryptedJson);
            var credential = JsonSerializer.Deserialize(json, CredentialJsonContext.Default.StoredCredential);
            return credential;
        }
        catch
        {
            return null;
        }
    }

    [RequiresDynamicCode("JSON serialization may require dynamic code generation")]
    [RequiresUnreferencedCode("JSON serialization may require unreferenced code")]
    public override async Task SaveCredentialAsync(string serverUrl, StoredCredential credential)
    {
        var filePath = GetCredentialFilePath(serverUrl);
        var json = JsonSerializer.Serialize(credential, CredentialJsonContext.Default.StoredCredential);
        var encryptedJson = EncryptString(json);

        await File.WriteAllTextAsync(filePath, encryptedJson);

        // ファイルパーミッションを600に設定（Unix系のみ）
        if (!OperatingSystem.IsWindows())
        {
            File.SetUnixFileMode(filePath, UnixFileMode.UserRead | UnixFileMode.UserWrite);
        }
    }

    public override async Task DeleteCredentialAsync(string serverUrl)
    {
        var filePath = GetCredentialFilePath(serverUrl);

        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }

        await Task.CompletedTask;
    }

    private string GetCredentialFilePath(string serverUrl)
    {
        var keyName = GetKeyName(serverUrl);
        // ファイル名として使用できない文字を置換
        var safeFileName = keyName.Replace(":", "_").Replace("/", "_").Replace("\\", "_");
        return Path.Combine(_credentialsDirectory, $"{safeFileName}.cred");
    }

    // 簡易的な暗号化（実際の実装ではより強固な暗号化を使用すべき）
    private string EncryptString(string plainText)
    {
        // Base64エンコードのみ（簡易実装）
        // TODO: 実際の実装では適切な暗号化を使用
        var bytes = Encoding.UTF8.GetBytes(plainText);
        return Convert.ToBase64String(bytes);
    }

    private string DecryptString(string encryptedText)
    {
        // Base64デコードのみ（簡易実装）
        // TODO: 実際の実装では適切な復号化を使用
        var bytes = Convert.FromBase64String(encryptedText);
        return Encoding.UTF8.GetString(bytes);
    }
}
