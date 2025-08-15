using System.Diagnostics;
using System.Runtime.InteropServices;

using Microsoft.Extensions.Logging;

using RedmineCLI.Models;
using RedmineCLI.Services;

namespace RedmineCLI.Extensions;

/// <summary>
/// Executes RedmineCLI extensions as separate processes
/// </summary>
public class ExtensionExecutor : IExtensionExecutor
{
    private readonly IConfigService _configService;
    private readonly ILogger<ExtensionExecutor> _logger;

    public ExtensionExecutor(IConfigService configService, ILogger<ExtensionExecutor> logger)
    {
        _configService = configService;
        _logger = logger;
    }

    public async Task<int> ExecuteAsync(string extensionName, string[] args)
    {
        var extensionPath = FindExtension(extensionName);
        if (extensionPath is null)
        {
            _logger.LogError($"Extension 'redmine-{extensionName}' not found.");
            return 1;
        }

        var config = await _configService.LoadConfigAsync();
        var profile = config.Profiles.TryGetValue(config.CurrentProfile, out var currentProfile) ? currentProfile : null;

        using var process = new Process();
        process.StartInfo = new ProcessStartInfo
        {
            FileName = extensionPath,
            Arguments = string.Join(" ", args.Select(arg => arg.Contains(' ') ? $"\"{arg}\"" : arg)),
            UseShellExecute = false,
            CreateNoWindow = true
        };

        // Set environment variables from current profile
        if (profile is not null)
        {
            SetEnvironmentVariable(process.StartInfo, "REDMINE_URL", profile.Url);
            SetEnvironmentVariable(process.StartInfo, "REDMINE_API_KEY", profile.ApiKey);

            if (!string.IsNullOrEmpty(profile.UserName))
                SetEnvironmentVariable(process.StartInfo, "REDMINE_USER", profile.UserName);

            if (!string.IsNullOrEmpty(profile.DefaultProject))
                SetEnvironmentVariable(process.StartInfo, "REDMINE_PROJECT", profile.DefaultProject);

            SetEnvironmentVariable(process.StartInfo, "REDMINE_TIME_FORMAT", profile.TimeFormat.ToString());
            SetEnvironmentVariable(process.StartInfo, "REDMINE_OUTPUT_FORMAT", profile.OutputFormat);
        }

        // Set config directory path
        var configDir = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "redmine")
            : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".config", "redmine");
        SetEnvironmentVariable(process.StartInfo, "REDMINE_CONFIG_DIR", configDir);

        try
        {
            _logger.LogDebug($"Executing extension: {extensionPath} {string.Join(" ", args)}");
            process.Start();
            await process.WaitForExitAsync();
            return process.ExitCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Failed to execute extension: {extensionPath}");
            return 1;
        }
    }

    public virtual string? FindExtension(string name)
    {
        var extensionName = $"redmine-{name}";
        var searchPaths = GetSearchPaths();

        foreach (var path in searchPaths)
        {
            if (!Directory.Exists(path))
                continue;

            // Check for extension with platform-specific executable extension
            var extensions = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? new[] { ".exe", ".cmd", ".bat" }
                : new[] { "", ".sh" };

            foreach (var ext in extensions)
            {
                var fullPath = Path.Combine(path, extensionName + ext);
                if (File.Exists(fullPath))
                {
                    // On Unix, check if file is executable
                    if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && ext == "")
                    {
                        try
                        {
                            var fileInfo = new UnixFileInfo(fullPath);
                            if ((fileInfo.FileAccessPermissions & FileAccessPermissions.UserExecute) != 0)
                                return fullPath;
                        }
                        catch
                        {
                            // If we can't check permissions, assume it's executable
                            return fullPath;
                        }
                    }
                    else
                    {
                        return fullPath;
                    }
                }
            }
        }

        return null;
    }

    public IEnumerable<string> ListExtensions()
    {
        var extensions = new HashSet<string>();
        var searchPaths = GetSearchPaths();

        foreach (var path in searchPaths)
        {
            if (!Directory.Exists(path))
                continue;

            var files = Directory.GetFiles(path, "redmine-*");
            foreach (var file in files)
            {
                var fileName = Path.GetFileNameWithoutExtension(file);
                if (fileName.StartsWith("redmine-"))
                {
                    var extensionName = fileName.Substring("redmine-".Length);

                    // On Windows, check if it's an executable
                    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    {
                        var ext = Path.GetExtension(file).ToLowerInvariant();
                        if (ext == ".exe" || ext == ".cmd" || ext == ".bat")
                            extensions.Add(extensionName);
                    }
                    else
                    {
                        // On Unix, check if file is executable
                        try
                        {
                            var fileInfo = new UnixFileInfo(file);
                            if ((fileInfo.FileAccessPermissions & FileAccessPermissions.UserExecute) != 0)
                                extensions.Add(extensionName);
                        }
                        catch
                        {
                            // If we can't check permissions, include it anyway
                            extensions.Add(extensionName);
                        }
                    }
                }
            }
        }

        return extensions.OrderBy(x => x);
    }

    private IEnumerable<string> GetSearchPaths()
    {
        var paths = new List<string>();

        // 1. User extensions directory
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            paths.Add(Path.Combine(localAppData, "redmine", "extensions"));
        }
        else
        {
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            paths.Add(Path.Combine(home, ".local", "share", "redmine", "extensions"));
        }

        // 2. Same directory as RedmineCLI executable
        var executableDir = AppContext.BaseDirectory;
        paths.Add(executableDir);

        // 3. PATH environment variable
        var pathEnv = Environment.GetEnvironmentVariable("PATH");
        if (!string.IsNullOrEmpty(pathEnv))
        {
            var separator = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? ';' : ':';
            paths.AddRange(pathEnv.Split(separator, StringSplitOptions.RemoveEmptyEntries));
        }

        return paths;
    }

    private static void SetEnvironmentVariable(ProcessStartInfo startInfo, string key, string value)
    {
        startInfo.Environment[key] = value;
    }

    // Placeholder for Unix file permissions check
    // In a real implementation, this would use platform-specific APIs
    private class UnixFileInfo
    {
        public FileAccessPermissions FileAccessPermissions { get; }

        public UnixFileInfo(string path)
        {
            // This is a simplified implementation
            // In reality, we'd use P/Invoke or a library to check Unix permissions
            FileAccessPermissions = FileAccessPermissions.UserExecute;
        }
    }

    [Flags]
    private enum FileAccessPermissions
    {
        None = 0,
        UserExecute = 0x40
    }
}
