using System.Diagnostics;
using System.Runtime.Versioning;
using System.Text;
using System.Text.Json;

using RedmineCLI.Common.Models;

namespace RedmineCLI.Common.Services;

/// <summary>
/// macOS Keychain を使用した認証情報ストア
/// </summary>
[SupportedOSPlatform("macos")]
public class MacOSCredentialStore : CredentialStore
{
    public override async Task<StoredCredential?> GetCredentialAsync(string serverUrl)
    {
        var keyName = GetKeyName(serverUrl);

        try
        {
            // security find-generic-password コマンドを実行
            var passwordResult = await ExecuteSecurityCommand(
                $"find-generic-password -s \"{keyName}\" -w");

            if (string.IsNullOrEmpty(passwordResult))
                return null;

            // JSON形式で保存されているデータをデシリアライズ
            var credential = JsonSerializer.Deserialize(passwordResult, CredentialJsonContext.Default.StoredCredential);
            return credential;
        }
        catch
        {
            return null;
        }
    }

    public override async Task SaveCredentialAsync(string serverUrl, StoredCredential credential)
    {
        var keyName = GetKeyName(serverUrl);
        var json = JsonSerializer.Serialize(credential, CredentialJsonContext.Default.StoredCredential);

        // 既存のエントリを削除
        await ExecuteSecurityCommand(
            $"delete-generic-password -s \"{keyName}\" 2>/dev/null");

        // 新規作成
        // Note: パスワードは標準入力経由で渡す必要がある
        var psi = new ProcessStartInfo
        {
            FileName = "/usr/bin/security",
            Arguments = $"add-generic-password -s \"{keyName}\" -a \"RedmineCLI\" -w \"{json.Replace("\"", "\\\"")}\" -U",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(psi);
        if (process != null)
        {
            await process.WaitForExitAsync();
        }
    }

    public override async Task DeleteCredentialAsync(string serverUrl)
    {
        var keyName = GetKeyName(serverUrl);
        await ExecuteSecurityCommand(
            $"delete-generic-password -s \"{keyName}\"");
    }

    private async Task<string?> ExecuteSecurityCommand(string args)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "/usr/bin/security",
            Arguments = args,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(psi);
        if (process == null)
            return null;

        var output = await process.StandardOutput.ReadToEndAsync();
        await process.WaitForExitAsync();

        return process.ExitCode == 0 ? output.Trim() : null;
    }
}
