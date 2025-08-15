using System;
using System.CommandLine;

namespace RedmineExample;

/// <summary>
/// Sample RedmineCLI extension demonstrating the extension system
/// </summary>
public class Program
{
    public static async Task<int> Main(string[] args)
    {
        var rootCommand = new RootCommand("Sample RedmineCLI extension");

        // Add a simple info command
        var infoCommand = new Command("info", "Display extension information");
        infoCommand.SetHandler(() =>
        {
            Console.WriteLine("RedmineCLI Example Extension v1.0.0");
            Console.WriteLine();
            Console.WriteLine("This is a sample extension demonstrating the RedmineCLI extension system.");
            Console.WriteLine();
            Console.WriteLine("Environment variables available:");
            
            // Display environment variables passed by RedmineCLI
            var envVars = new[]
            {
                "REDMINE_URL",
                "REDMINE_API_KEY",
                "REDMINE_USER",
                "REDMINE_PROJECT",
                "REDMINE_CONFIG_DIR",
                "REDMINE_TIME_FORMAT",
                "REDMINE_OUTPUT_FORMAT"
            };

            foreach (var envVar in envVars)
            {
                var value = Environment.GetEnvironmentVariable(envVar);
                if (!string.IsNullOrEmpty(value))
                {
                    // Mask API key for security
                    if (envVar == "REDMINE_API_KEY")
                    {
                        value = value.Length > 4 ? value.Substring(0, 4) + "..." : "***";
                    }
                    Console.WriteLine($"  {envVar}: {value}");
                }
            }
        });

        // Add a test command
        var testCommand = new Command("test", "Test Redmine API access");
        testCommand.SetHandler(async () =>
        {
            var url = Environment.GetEnvironmentVariable("REDMINE_URL");
            var apiKey = Environment.GetEnvironmentVariable("REDMINE_API_KEY");

            if (string.IsNullOrEmpty(url) || string.IsNullOrEmpty(apiKey))
            {
                Console.Error.WriteLine("Error: REDMINE_URL and REDMINE_API_KEY must be configured");
                Environment.Exit(1);
            }

            Console.WriteLine($"Testing connection to {url}...");
            
            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("X-Redmine-API-Key", apiKey);

            try
            {
                var response = await httpClient.GetAsync($"{url}/users/current.json");
                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine("✓ Successfully connected to Redmine API");
                    Console.WriteLine($"  Status: {response.StatusCode}");
                }
                else
                {
                    Console.Error.WriteLine($"✗ Failed to connect: {response.StatusCode}");
                    Environment.Exit(1);
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"✗ Connection error: {ex.Message}");
                Environment.Exit(1);
            }
        });

        rootCommand.AddCommand(infoCommand);
        rootCommand.AddCommand(testCommand);

        return await rootCommand.InvokeAsync(args);
    }
}