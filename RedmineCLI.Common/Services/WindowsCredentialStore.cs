using System.Diagnostics.CodeAnalysis;
using System.Runtime.Versioning;
using System.Text;
using System.Text.Json;

using RedmineCLI.Common.Models;

namespace RedmineCLI.Common.Services;

/// <summary>
/// Windows Credential Manager を使用した認証情報ストア
/// </summary>
[SupportedOSPlatform("windows")]
public class WindowsCredentialStore : CredentialStore
{
    [RequiresDynamicCode("JSON serialization may require dynamic code generation")]
    [RequiresUnreferencedCode("JSON serialization may require unreferenced code")]
    public override async Task<StoredCredential?> GetCredentialAsync(string serverUrl)
    {
        // 実際の実装では Windows Credential Manager API を使用
        // ここでは簡略化のため、メモリ内実装
        await Task.CompletedTask;

        // TODO: P/Invoke を使用して実装
        // CredRead API を呼び出し

        return null;
    }

    [RequiresDynamicCode("JSON serialization may require dynamic code generation")]
    [RequiresUnreferencedCode("JSON serialization may require unreferenced code")]
    public override async Task SaveCredentialAsync(string serverUrl, StoredCredential credential)
    {
        // 実際の実装では Windows Credential Manager API を使用
        await Task.CompletedTask;

        // TODO: P/Invoke を使用して実装
        // CredWrite API を呼び出し

        var keyName = GetKeyName(serverUrl);
        var json = JsonSerializer.Serialize(credential);
        var bytes = Encoding.UTF8.GetBytes(json);

        // Windows Credential Manager に保存
    }

    public override async Task DeleteCredentialAsync(string serverUrl)
    {
        // 実際の実装では Windows Credential Manager API を使用
        await Task.CompletedTask;

        // TODO: P/Invoke を使用して実装
        // CredDelete API を呼び出し

        var keyName = GetKeyName(serverUrl);
        // Windows Credential Manager から削除
    }
}
