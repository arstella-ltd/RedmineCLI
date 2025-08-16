using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Versioning;
using System.Text;
using System.Text.Json;

using RedmineCLI.Common.Models;

namespace RedmineCLI.Common.Services;

/// <summary>
/// Linux libsecret (Secret Service API) を使用した認証情報ストア
/// </summary>
[SupportedOSPlatform("linux")]
public class LinuxCredentialStore : CredentialStore
{
    private static bool? _isSecretToolAvailable;

    /// <summary>
    /// secret-toolが利用可能かチェック（同期版）
    /// </summary>
    public static bool IsSecretToolAvailable()
    {
        if (_isSecretToolAvailable.HasValue)
            return _isSecretToolAvailable.Value;

        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "secret-tool",
                Arguments = "--version",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(psi);
            if (process != null)
            {
                process.WaitForExit();
                _isSecretToolAvailable = process.ExitCode == 0;
                return _isSecretToolAvailable.Value;
            }
        }
        catch
        {
            // secret-toolが見つからない場合
        }

        _isSecretToolAvailable = false;
        return false;
    }
    [RequiresUnreferencedCode("JSON serialization may require unreferenced code")]
    [RequiresDynamicCode("JSON serialization may require dynamic code generation")]
    public override async Task<StoredCredential?> GetCredentialAsync(string serverUrl)
    {
        var keyName = GetKeyName(serverUrl);

        try
        {
            // secret-tool lookup コマンドを実行
            var result = await ExecuteCommand(
                "secret-tool",
                $"lookup service RedmineCLI server \"{keyName}\"");

            if (string.IsNullOrEmpty(result))
                return null;

            // JSON形式で保存されているデータをデシリアライズ
            var credential = JsonSerializer.Deserialize<StoredCredential>(result);
            return credential;
        }
        catch
        {
            return null;
        }
    }

    [RequiresUnreferencedCode("JSON serialization may require unreferenced code")]
    [RequiresDynamicCode("JSON serialization may require dynamic code generation")]
    public override async Task SaveCredentialAsync(string serverUrl, StoredCredential credential)
    {
        var keyName = GetKeyName(serverUrl);
        var json = JsonSerializer.Serialize(credential);

        try
        {
            // secret-tool store コマンドを実行
            var psi = new ProcessStartInfo
            {
                FileName = "secret-tool",
                Arguments = $"store --label=\"RedmineCLI:{keyName}\" service RedmineCLI server \"{keyName}\"",
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(psi);
            if (process != null)
            {
                await process.StandardInput.WriteLineAsync(json);
                await process.StandardInput.FlushAsync();
                process.StandardInput.Close();
                await process.WaitForExitAsync();

                if (process.ExitCode != 0)
                {
                    throw new InvalidOperationException($"secret-tool failed with exit code {process.ExitCode}");
                }
            }
            else
            {
                throw new InvalidOperationException("Failed to start secret-tool process");
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                "Failed to save credentials. Ensure 'secret-tool' (libsecret) is installed on your system.", ex);
        }
    }

    public override async Task DeleteCredentialAsync(string serverUrl)
    {
        var keyName = GetKeyName(serverUrl);

        try
        {
            // secret-tool clear コマンドを実行
            await ExecuteCommand(
                "secret-tool",
                $"clear service RedmineCLI server \"{keyName}\"");
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                "Failed to delete credentials. Ensure 'secret-tool' (libsecret) is installed on your system.", ex);
        }
    }

    private async Task<string?> ExecuteCommand(string command, string args)
    {
        var psi = new ProcessStartInfo
        {
            FileName = command,
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
