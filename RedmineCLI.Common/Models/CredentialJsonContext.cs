using System.Text.Json.Serialization;

namespace RedmineCLI.Common.Models;

/// <summary>
/// JSON Source Generator context for credential serialization
/// </summary>
[JsonSourceGenerationOptions(WriteIndented = false)]
[JsonSerializable(typeof(StoredCredential))]
public partial class CredentialJsonContext : JsonSerializerContext
{
}