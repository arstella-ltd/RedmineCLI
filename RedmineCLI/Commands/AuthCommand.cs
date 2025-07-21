using System.CommandLine;
using System.Diagnostics.CodeAnalysis;

using Microsoft.Extensions.Logging;

using RedmineCLI.ApiClient;
using RedmineCLI.Services;

using Spectre.Console;

using Profile = RedmineCLI.Models.Profile;

namespace RedmineCLI.Commands;

public class AuthCommand
{
    private readonly IConfigService _configService;
    private readonly IRedmineApiClient _apiClient;
    private readonly ILogger<AuthCommand> _logger;

    public AuthCommand(IConfigService configService, IRedmineApiClient apiClient, ILogger<AuthCommand> logger)
    {
        _configService = configService;
        _apiClient = apiClient;
        _logger = logger;
    }

    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(AuthCommand))]
    public static Command Create(IConfigService configService, IRedmineApiClient apiClient, ILogger<AuthCommand> logger)
    {
        var authCommand = new AuthCommand(configService, apiClient, logger);

        var command = new Command("auth", "Authenticate with Redmine server");

        // Login subcommand
        var loginCommand = new Command("login", "Login to Redmine server");
        var urlOption = new Option<string>("--url") { Description = "Redmine server URL" };
        var apiKeyOption = new Option<string>("--api-key") { Description = "Redmine API key" };
        var profileOption = new Option<string>("--profile") { Description = "Profile name", DefaultValueFactory = _ => "default" };

        loginCommand.Add(urlOption);
        loginCommand.Add(apiKeyOption);
        loginCommand.Add(profileOption);

        loginCommand.SetAction(async (parseResult) =>
        {
            var url = parseResult.GetValue(urlOption);
            var apiKey = parseResult.GetValue(apiKeyOption);
            var profile = parseResult.GetValue(profileOption) ?? "default";

            if (string.IsNullOrEmpty(url) || string.IsNullOrEmpty(apiKey))
            {
                Environment.ExitCode = await authCommand.LoginInteractiveAsync();
            }
            else
            {
                Environment.ExitCode = await authCommand.LoginAsync(url, apiKey, profile);
            }
        });

        // Status subcommand
        var statusCommand = new Command("status", "Show authentication status");
        statusCommand.SetAction(async (parseResult) =>
        {
            Environment.ExitCode = await authCommand.StatusAsync();
        });

        // Logout subcommand
        var logoutCommand = new Command("logout", "Logout from Redmine server");
        logoutCommand.SetAction(async (parseResult) =>
        {
            Environment.ExitCode = await authCommand.LogoutAsync();
        });

        command.Add(loginCommand);
        command.Add(statusCommand);
        command.Add(logoutCommand);

        return command;
    }

    public async Task<int> LoginAsync(string url, string apiKey, string profileName)
    {
        try
        {
            // URL validation
            if (!IsValidUrl(url, out var validationError))
            {
                DisplayError(validationError);
                return 1;
            }

            _logger.LogDebug("Testing connection to {Url}", url);

            // Test connection with provided credentials
            var connectionTest = await _apiClient.TestConnectionAsync(url, apiKey);
            if (!connectionTest)
            {
                DisplayError("Failed to connect to Redmine server. Please check your URL and API key.");
                return 1;
            }

            // Only save config after successful connection
            var config = await _configService.LoadConfigAsync();

            var profile = new Profile
            {
                Name = profileName,
                Url = url,
                ApiKey = apiKey
            };

            config.Profiles[profileName] = profile;
            config.CurrentProfile = profileName;
            await _configService.SaveConfigAsync(config);

            DisplaySuccess($"Successfully authenticated with Redmine server");
            AnsiConsole.MarkupLine($"Profile '[cyan]{profileName}[/]' has been configured");
            AnsiConsole.MarkupLine($"Server URL: [yellow]{url}[/]");

            return 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login process");
            DisplayError("An unexpected error occurred during login");
            return 1;
        }
    }

    public async Task<int> LoginInteractiveAsync()
    {
        try
        {
            AnsiConsole.MarkupLine("[bold cyan]Redmine CLI Authentication Setup[/]");
            AnsiConsole.WriteLine();

            // Prompt for URL
            var url = AnsiConsole.Prompt(
                new TextPrompt<string>("Enter your Redmine server URL:")
                    .ValidationErrorMessage("[red]Please enter a valid URL[/]")
                    .Validate(input =>
                    {
                        if (!IsValidUrl(input, out var error))
                            return ValidationResult.Error(error);

                        return ValidationResult.Success();
                    }));

            // Prompt for API key
            var apiKey = AnsiConsole.Prompt(
                new TextPrompt<string>("Enter your Redmine API key:")
                    .Secret()
                    .ValidationErrorMessage("[red]API key cannot be empty[/]")
                    .Validate(input =>
                    {
                        if (string.IsNullOrWhiteSpace(input))
                            return ValidationResult.Error("API key cannot be empty");
                        return ValidationResult.Success();
                    }));

            // Prompt for profile name
            var profileName = AnsiConsole.Prompt(
                new TextPrompt<string>("Enter profile name:")
                    .DefaultValue("default")
                    .ValidationErrorMessage("[red]Profile name cannot be empty[/]")
                    .Validate(input =>
                    {
                        if (string.IsNullOrWhiteSpace(input))
                            return ValidationResult.Error("Profile name cannot be empty");
                        return ValidationResult.Success();
                    }));

            return await LoginAsync(url, apiKey, profileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during interactive login");
            DisplayError("An unexpected error occurred during login setup");
            return 1;
        }
    }

    public async Task<int> StatusAsync()
    {
        try
        {
            var activeProfile = await _configService.GetActiveProfileAsync();

            if (activeProfile == null)
            {
                DisplayWarning("Not authenticated");
                AnsiConsole.MarkupLine("Run [cyan]redmine auth login[/] to authenticate");
                return 1;
            }

            AnsiConsole.MarkupLine("[bold]Authentication Status[/]");
            AnsiConsole.WriteLine();

            var table = new Table();
            table.AddColumn("Property");
            table.AddColumn("Value");

            table.AddRow("Profile", $"[cyan]{activeProfile.Name}[/]");
            table.AddRow("Server URL", $"[yellow]{activeProfile.Url}[/]");
            table.AddRow("API Key", $"[dim]{MaskApiKey(activeProfile.ApiKey)}[/]");

            if (!string.IsNullOrEmpty(activeProfile.DefaultProject))
            {
                table.AddRow("Default Project", $"[green]{activeProfile.DefaultProject}[/]");
            }

            AnsiConsole.Write(table);

            // Only test connection if API key is set
            if (!string.IsNullOrEmpty(activeProfile.ApiKey))
            {
                AnsiConsole.WriteLine();

                // Test connection
                var connectionSuccess = await AnsiConsole.Status()
                    .StartAsync<bool>("Testing connection...", async ctx =>
                    {
                        ctx.Spinner(Spinner.Known.Dots);
                        var connectionTest = await _apiClient.TestConnectionAsync();

                        if (connectionTest)
                        {
                            AnsiConsole.MarkupLine("[green]✓ Connection successful[/]");
                            return true;
                        }
                        else
                        {
                            AnsiConsole.MarkupLine("[red]✗ Connection failed[/]");
                            AnsiConsole.MarkupLine("Please check your credentials or run [cyan]redmine auth login[/] again");
                            return false;
                        }
                    });

                return connectionSuccess ? 0 : 1;
            }
            else
            {
                AnsiConsole.WriteLine();
                AnsiConsole.MarkupLine("[yellow]No API key configured[/]");
                AnsiConsole.MarkupLine("Run [cyan]redmine auth login[/] to authenticate");
                return 1;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking authentication status");
            DisplayError("Failed to check authentication status");
            return 1;
        }
    }

    public async Task<int> LogoutAsync()
    {
        try
        {
            var activeProfile = await _configService.GetActiveProfileAsync();

            if (activeProfile == null)
            {
                DisplayWarning("Not currently authenticated");
                return 1;
            }

            var config = await _configService.LoadConfigAsync();

            // Clear API key but preserve other settings
            if (config.Profiles.ContainsKey(activeProfile.Name))
            {
                config.Profiles[activeProfile.Name].ApiKey = string.Empty;
                await _configService.SaveConfigAsync(config);
            }

            DisplaySuccess($"Logged out from profile '[cyan]{activeProfile.Name}[/]'");
            AnsiConsole.MarkupLine("Run [cyan]redmine auth login[/] to authenticate again");

            return 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during logout");
            DisplayError("Failed to logout");
            return 1;
        }
    }

    private static string MaskApiKey(string apiKey)
    {
        if (string.IsNullOrEmpty(apiKey))
            return "[red]Not set[/]";

        if (apiKey.Length <= 8)
            return new string('*', apiKey.Length);

        return apiKey[..4] + new string('*', apiKey.Length - 8) + apiKey[^4..];
    }

    private static bool IsValidUrl(string url, out string errorMessage)
    {
        errorMessage = string.Empty;

        if (string.IsNullOrWhiteSpace(url))
        {
            errorMessage = "URL cannot be empty";
            return false;
        }

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) ||
            (uri.Scheme != "http" && uri.Scheme != "https"))
        {
            errorMessage = "Invalid URL format. Please provide a valid HTTP/HTTPS URL.";
            return false;
        }

        return true;
    }

    private static void DisplayError(string message)
    {
        AnsiConsole.MarkupLine($"[red]Error: {message}[/]");
    }

    private static void DisplaySuccess(string message)
    {
        AnsiConsole.MarkupLine($"[green]✓[/] {message}");
    }

    private static void DisplayWarning(string message)
    {
        AnsiConsole.MarkupLine($"[yellow]{message}[/]");
    }
}
