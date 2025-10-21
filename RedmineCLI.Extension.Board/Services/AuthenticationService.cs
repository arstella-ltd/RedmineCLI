using System.Text.RegularExpressions;

using Microsoft.Extensions.Logging;

using RedmineCLI.Common.Models;
using RedmineCLI.Common.Services;

using Spectre.Console;

namespace RedmineCLI.Extension.Board.Services;

/// <summary>
/// 認証とセッション管理のためのサービス実装
/// </summary>
public class AuthenticationService : IAuthenticationService
{
    private readonly ILogger<AuthenticationService> _logger;
    private readonly ICredentialStore _credentialStore;

    public AuthenticationService(ILogger<AuthenticationService> logger, ICredentialStore credentialStore)
    {
        _logger = logger;
        _credentialStore = credentialStore;
    }

    public async Task<(string url, string? sessionCookie)> GetAuthenticationAsync(string? urlOverride)
    {
        // _logger.LogDebug("Getting authentication information");

        // Determine Redmine URL
        string? redmineUrl = urlOverride;

        if (string.IsNullOrEmpty(redmineUrl))
        {
            redmineUrl = Environment.GetEnvironmentVariable("REDMINE_URL");
        }

        if (string.IsNullOrEmpty(redmineUrl))
        {
            // Try to get from stored config
            var configDir = Environment.GetEnvironmentVariable("REDMINE_CONFIG_DIR") ??
                           Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "redmine");
            var configFile = Path.Combine(configDir, "config.yml");

            if (File.Exists(configFile))
            {
                try
                {
                    var configContent = await File.ReadAllTextAsync(configFile);
                    // Simple YAML parsing for URL
                    var urlMatch = Regex.Match(configContent, @"url:\s*(.+)");
                    if (urlMatch.Success)
                    {
                        redmineUrl = urlMatch.Groups[1].Value.Trim();
                        // Remove quotes if present (YAML string values)
                        redmineUrl = redmineUrl.Trim('"', '\'');
                        // _logger.LogDebug("Found URL in config file: {Url}", redmineUrl);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Could not read config file");
                }
            }
        }

        if (string.IsNullOrEmpty(redmineUrl))
        {
            _logger.LogError("No Redmine URL configured");
            AnsiConsole.MarkupLine("[red]Error: No Redmine URL found.[/]");
            AnsiConsole.MarkupLine("Please specify [cyan]--url[/] or run [cyan]redmine auth login[/] first.");
            Environment.ExitCode = 1;
            return (string.Empty, null);
        }

        // Normalize URL - ensure it has a scheme
        redmineUrl = redmineUrl.TrimEnd('/');
        if (!redmineUrl.StartsWith("http://") && !redmineUrl.StartsWith("https://"))
        {
            // Default to https if no scheme is provided
            redmineUrl = $"https://{redmineUrl}";
            // _logger.LogDebug("Added https:// scheme to URL");
        }

        // _logger.LogDebug("Using Redmine URL: {Url}", redmineUrl);

        // Get credentials from OS keychain
        try
        {
            var credential = await _credentialStore.GetCredentialAsync(redmineUrl);
            if (credential == null)
            {
                _logger.LogError("No credentials found in keychain for {Url}", redmineUrl);
                AnsiConsole.MarkupLine($"[red]Error: No credentials found for {redmineUrl}.[/]");
                AnsiConsole.MarkupLine("Please run [cyan]redmine auth login --save-password[/] first.");
                Environment.ExitCode = 1;
                return (redmineUrl, null);
            }

            // _logger.LogDebug("Found credentials for {Url}", redmineUrl);

            // Try to get or create session
            var sessionCookie = await Common.Authentication.AuthenticationHelper.CreateSessionFromCredentialsAsync(
                redmineUrl,
                credential,
                _logger);

            if (!string.IsNullOrEmpty(sessionCookie))
            {
                // _logger.LogInformation("Successfully authenticated with Redmine");

                // Update stored credential with new session if it changed
                if (sessionCookie != credential.SessionCookie)
                {
                    credential.SessionCookie = sessionCookie;
                    credential.SessionExpiry = DateTime.UtcNow.AddHours(24);
                    await _credentialStore.SaveCredentialAsync(redmineUrl, credential);
                    // _logger.LogDebug("Updated session cookie in keychain");
                }

                return (redmineUrl, sessionCookie);
            }

            // Fallback: if no new session could be created, but an existing cookie is available,
            // return it to allow callers to attempt with the stored session.
            if (!string.IsNullOrEmpty(credential.SessionCookie))
            {
                return (redmineUrl, credential.SessionCookie);
            }

            // If we have API key but no session, we can still try with API key
            if (!string.IsNullOrEmpty(credential.ApiKey))
            {
                _logger.LogWarning("No session cookie available, will try with API key");
                return (redmineUrl, null);
            }

            _logger.LogError("Failed to create session from stored credentials");
            AnsiConsole.MarkupLine("[red]Error: Failed to authenticate.[/]");
            AnsiConsole.MarkupLine("Please run [cyan]redmine auth login --save-password[/] again.");
            Environment.ExitCode = 1;
            return (redmineUrl, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error accessing keychain");
            AnsiConsole.MarkupLine($"[red]Error accessing keychain: {ex.Message}[/]");
            AnsiConsole.MarkupLine("Please run [cyan]redmine auth login --save-password[/] first.");
            Environment.ExitCode = 1;
            return (redmineUrl ?? string.Empty, null);
        }
    }
}
