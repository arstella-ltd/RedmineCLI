using System.Net.Http;
using System.Net.Http.Headers;

using Microsoft.Extensions.Logging;

using Polly;
using Polly.Extensions.Http;

namespace RedmineCLI.Common.Http;

/// <summary>
/// Factory for creating HttpClient instances with standard configuration for Redmine API access
/// </summary>
public class RedmineHttpClientFactory
{
    private const int DefaultTimeoutSeconds = 30;
    private const int RetryCount = 3;
    private const string DefaultUserAgent = "RedmineCLI/1.0";

    private readonly ILogger<RedmineHttpClientFactory>? _logger;

    public RedmineHttpClientFactory(ILogger<RedmineHttpClientFactory>? logger = null)
    {
        _logger = logger;
    }

    /// <summary>
    /// Creates an HttpClient configured for Redmine API access
    /// </summary>
    /// <param name="baseUrl">The base URL of the Redmine instance</param>
    /// <param name="apiKey">Optional API key for authentication</param>
    /// <param name="userAgent">Optional custom user agent string</param>
    /// <param name="timeoutSeconds">Optional timeout in seconds</param>
    public HttpClient CreateClient(
        string? baseUrl = null,
        string? apiKey = null,
        string? userAgent = null,
        int timeoutSeconds = DefaultTimeoutSeconds)
    {
        var httpClient = new HttpClient();

        // Set base address if provided
        if (!string.IsNullOrEmpty(baseUrl))
        {
            httpClient.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/");
        }

        // Set timeout
        httpClient.Timeout = TimeSpan.FromSeconds(timeoutSeconds);

        // Set default headers
        httpClient.DefaultRequestHeaders.Add("User-Agent", userAgent ?? DefaultUserAgent);
        httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        // Add API key if provided
        if (!string.IsNullOrEmpty(apiKey))
        {
            httpClient.DefaultRequestHeaders.Add("X-Redmine-API-Key", apiKey);
        }

        _logger?.LogDebug("Created HttpClient with base URL: {BaseUrl}, timeout: {Timeout}s",
            baseUrl ?? "(none)", timeoutSeconds);

        return httpClient;
    }

    /// <summary>
    /// Creates an HttpClient with session cookie authentication
    /// </summary>
    /// <param name="baseUrl">The base URL of the Redmine instance</param>
    /// <param name="sessionCookie">The session cookie value</param>
    /// <param name="userAgent">Optional custom user agent string</param>
    /// <param name="timeoutSeconds">Optional timeout in seconds</param>
    public HttpClient CreateClientWithSession(
        string baseUrl,
        string sessionCookie,
        string? userAgent = null,
        int timeoutSeconds = DefaultTimeoutSeconds)
    {
        var httpClient = CreateClient(baseUrl, null, userAgent, timeoutSeconds);

        // Add session cookie
        if (!string.IsNullOrEmpty(sessionCookie))
        {
            httpClient.DefaultRequestHeaders.Add("Cookie", sessionCookie);
        }

        _logger?.LogDebug("Created HttpClient with session cookie authentication");

        return httpClient;
    }

    /// <summary>
    /// Creates a retry policy for HTTP requests
    /// </summary>
    public static IAsyncPolicy<HttpResponseMessage> CreateRetryPolicy(ILogger? logger = null)
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .OrResult(msg => !msg.IsSuccessStatusCode && (int)msg.StatusCode >= 500)
            .WaitAndRetryAsync(
                RetryCount,
                retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                onRetry: (outcome, timespan, retryCount, context) =>
                {
                    var statusCode = outcome.Result?.StatusCode;
                    logger?.LogWarning(
                        "Request failed with {StatusCode}. Waiting {Timespan}ms before retry #{RetryCount}",
                        statusCode,
                        timespan.TotalMilliseconds,
                        retryCount);
                });
    }
}
