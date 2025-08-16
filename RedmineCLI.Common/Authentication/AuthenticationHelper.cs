using System.Net.Http;
using System.Text.RegularExpressions;

using Microsoft.Extensions.Logging;

using RedmineCLI.Common.Http;
using RedmineCLI.Common.Models;

namespace RedmineCLI.Common.Authentication;

/// <summary>
/// Helper class for handling authentication with Redmine
/// </summary>
public static class AuthenticationHelper
{
    /// <summary>
    /// Create a session cookie from stored credentials
    /// </summary>
    public static async Task<string?> CreateSessionFromCredentialsAsync(
        string redmineUrl,
        StoredCredential credential,
        ILogger? logger)
    {
        // If we already have a valid session cookie, use it
        if (credential.HasValidSession())
        {
            logger?.LogDebug("Using existing valid session cookie");
            return credential.SessionCookie;
        }

        // If we have username/password, try form login
        if (credential.HasPasswordCredentials())
        {
            logger?.LogInformation("Creating new session using form login");
            return await FormLoginAsync(redmineUrl, credential.Username!, credential.Password!, logger);
        }

        // If we only have API key, we can't create a session cookie
        if (!string.IsNullOrEmpty(credential.ApiKey))
        {
            logger?.LogWarning("Only API key available, cannot create session cookie");
            return null;
        }

        logger?.LogError("No valid credentials available");
        return null;
    }

    /// <summary>
    /// Perform form-based login to get a session cookie
    /// </summary>
    private static async Task<string?> FormLoginAsync(
        string redmineUrl,
        string username,
        string password,
        ILogger? logger)
    {
        redmineUrl = redmineUrl.TrimEnd('/');

        // Use the factory to create HttpClient
        var factory = new RedmineHttpClientFactory(null);
        using var httpClient = factory.CreateClient(redmineUrl, userAgent: "RedmineCLI/1.0");

        try
        {
            logger?.LogDebug("Step 1: Fetching login page to get authenticity token");

            // Step 1: Get the login page to extract authenticity token
            var loginPageUrl = $"{redmineUrl}/login";
            logger?.LogDebug("Requesting: {Url}", loginPageUrl);

            var loginPageResponse = await httpClient.GetAsync(loginPageUrl);
            logger?.LogDebug("Login page response status: {StatusCode}", loginPageResponse.StatusCode);

            if (!loginPageResponse.IsSuccessStatusCode)
            {
                logger?.LogError("Failed to fetch login page: {StatusCode}", loginPageResponse.StatusCode);
                return null;
            }

            var loginPageContent = await loginPageResponse.Content.ReadAsStringAsync();
            logger?.LogDebug("Login page content length: {Length} bytes", loginPageContent.Length);

            // Extract authenticity token from HTML
            var tokenMatch = Regex.Match(loginPageContent, @"name=""authenticity_token""\s+value=""([^""]+)""");
            if (!tokenMatch.Success)
            {
                logger?.LogError("Could not find authenticity_token in login page");
                return null;
            }

            var authenticityToken = tokenMatch.Groups[1].Value;
            logger?.LogDebug("Extracted authenticity token: {Token}",
                authenticityToken.Length > 10 ? authenticityToken[..10] + "..." : authenticityToken);

            // Extract session cookie from response
            string? sessionCookie = null;
            if (loginPageResponse.Headers.TryGetValues("Set-Cookie", out var setCookieHeaders))
            {
                foreach (var cookie in setCookieHeaders)
                {
                    if (cookie.StartsWith("_redmine_session="))
                    {
                        var endIndex = cookie.IndexOf(';');
                        sessionCookie = endIndex > 0 ? cookie[..endIndex] : cookie;
                        logger?.LogDebug("Extracted session cookie from login page");
                        break;
                    }
                }
            }

            logger?.LogDebug("Step 2: Submitting login form");

            // Step 2: Submit login form
            var loginData = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("authenticity_token", authenticityToken),
                new KeyValuePair<string, string>("back_url", redmineUrl),
                new KeyValuePair<string, string>("username", username),
                new KeyValuePair<string, string>("password", password),
                new KeyValuePair<string, string>("login", "Login »"),
                new KeyValuePair<string, string>("autologin", "1")
            });

            var loginRequest = new HttpRequestMessage(HttpMethod.Post, loginPageUrl);
            loginRequest.Content = loginData;
            if (!string.IsNullOrEmpty(sessionCookie))
            {
                loginRequest.Headers.Add("Cookie", sessionCookie);
            }
            loginRequest.Headers.Add("Referer", loginPageUrl);

            logger?.LogDebug("Sending login request to: {Url}", loginPageUrl);
            var loginResponse = await httpClient.SendAsync(loginRequest);

            logger?.LogDebug("Login response status: {StatusCode}", loginResponse.StatusCode);

            // Extract session cookie from login response
            string? newSessionCookie = null;
            if (loginResponse.Headers.TryGetValues("Set-Cookie", out var loginCookies))
            {
                foreach (var cookie in loginCookies)
                {
                    if (cookie.StartsWith("_redmine_session="))
                    {
                        var endIdx = cookie.IndexOf(';');
                        newSessionCookie = endIdx > 0 ? cookie[..endIdx] : cookie;
                        logger?.LogDebug("Extracted new session cookie from login response");
                        break;
                    }
                }
            }

            // Check if login was successful
            if (loginResponse.StatusCode == System.Net.HttpStatusCode.Found ||
                loginResponse.StatusCode == System.Net.HttpStatusCode.Redirect ||
                loginResponse.StatusCode == System.Net.HttpStatusCode.OK)
            {
                // If we got a redirect or OK, and have a session cookie, we're probably logged in
                if (!string.IsNullOrEmpty(newSessionCookie))
                {
                    logger?.LogInformation("Login successful, session created");
                    return newSessionCookie;
                }

                // Check response content for success indicators
                if (loginResponse.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    var responseContent = await loginResponse.Content.ReadAsStringAsync();
                    if (responseContent.Contains("Invalid user or password") ||
                        responseContent.Contains("flash error") ||
                        responseContent.Contains("id=\"flash_error\""))
                    {
                        logger?.LogError("Login failed: Invalid credentials");
                        return null;
                    }

                    if (responseContent.Contains("My account") || responseContent.Contains("Logged in as"))
                    {
                        logger?.LogInformation("Login successful based on content");
                        return newSessionCookie ?? sessionCookie;
                    }
                }
            }

            logger?.LogError("Login failed with status: {StatusCode}", loginResponse.StatusCode);
            return null;
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Error during form login");
            return null;
        }
    }
}
