namespace RedmineCLI.Extensions;

/// <summary>
/// Interface for executing RedmineCLI extensions
/// </summary>
public interface IExtensionExecutor
{
    /// <summary>
    /// Executes an extension with the given arguments
    /// </summary>
    /// <param name="extensionName">Name of the extension (without redmine- prefix)</param>
    /// <param name="args">Arguments to pass to the extension</param>
    /// <returns>Exit code from the extension process</returns>
    Task<int> ExecuteAsync(string extensionName, string[] args);

    /// <summary>
    /// Finds the full path to an extension executable
    /// </summary>
    /// <param name="name">Name of the extension (without redmine- prefix)</param>
    /// <returns>Full path to the extension executable, or null if not found</returns>
    string? FindExtension(string name);

    /// <summary>
    /// Lists all installed extensions
    /// </summary>
    /// <returns>List of installed extension names (without redmine- prefix)</returns>
    IEnumerable<string> ListExtensions();
}
