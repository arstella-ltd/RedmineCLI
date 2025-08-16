using System.Net.Http;

using Microsoft.Extensions.Logging;

using NSubstitute;

using RedmineCLI.Common.Http;

using Xunit;

namespace RedmineCLI.Common.Tests.Http;

public class RedmineHttpClientFactoryTests
{
    private readonly ILogger<RedmineHttpClientFactory> _logger;
    private readonly RedmineHttpClientFactory _factory;

    public RedmineHttpClientFactoryTests()
    {
        _logger = Substitute.For<ILogger<RedmineHttpClientFactory>>();
        _factory = new RedmineHttpClientFactory(_logger);
    }

    [Fact]
    public void CreateClient_WithBaseUrl_SetsBaseAddress()
    {
        // Arrange
        var baseUrl = "https://redmine.example.com";

        // Act
        var client = _factory.CreateClient(baseUrl);

        // Assert
        Assert.NotNull(client);
        Assert.NotNull(client.BaseAddress);
        Assert.Equal("https://redmine.example.com/", client.BaseAddress.ToString());
    }

    [Fact]
    public void CreateClient_WithApiKey_AddsApiKeyHeader()
    {
        // Arrange
        var apiKey = "test-api-key";

        // Act
        var client = _factory.CreateClient(apiKey: apiKey);

        // Assert
        Assert.NotNull(client);
        Assert.True(client.DefaultRequestHeaders.Contains("X-Redmine-API-Key"));
        var apiKeyValues = client.DefaultRequestHeaders.GetValues("X-Redmine-API-Key");
        Assert.Contains(apiKey, apiKeyValues);
    }

    [Fact]
    public void CreateClient_WithCustomUserAgent_SetsUserAgent()
    {
        // Arrange
        var userAgent = "CustomAgent/1.0";

        // Act
        var client = _factory.CreateClient(userAgent: userAgent);

        // Assert
        Assert.NotNull(client);
        Assert.True(client.DefaultRequestHeaders.Contains("User-Agent"));
        var userAgentValues = client.DefaultRequestHeaders.GetValues("User-Agent");
        Assert.Contains(userAgent, userAgentValues);
    }

    [Fact]
    public void CreateClient_WithCustomTimeout_SetsTimeout()
    {
        // Arrange
        var timeoutSeconds = 60;

        // Act
        var client = _factory.CreateClient(timeoutSeconds: timeoutSeconds);

        // Assert
        Assert.NotNull(client);
        Assert.Equal(TimeSpan.FromSeconds(timeoutSeconds), client.Timeout);
    }

    [Fact]
    public void CreateClientWithSession_WithSessionCookie_AddsCookieHeader()
    {
        // Arrange
        var baseUrl = "https://redmine.example.com";
        var sessionCookie = "_redmine_session=abc123";

        // Act
        var client = _factory.CreateClientWithSession(baseUrl, sessionCookie);

        // Assert
        Assert.NotNull(client);
        Assert.True(client.DefaultRequestHeaders.Contains("Cookie"));
        var cookieValues = client.DefaultRequestHeaders.GetValues("Cookie");
        Assert.Contains(sessionCookie, cookieValues);
    }

    [Fact]
    public void CreateClientWithSession_WithCustomUserAgent_SetsUserAgent()
    {
        // Arrange
        var baseUrl = "https://redmine.example.com";
        var sessionCookie = "_redmine_session=abc123";
        var userAgent = "BoardExtension/2.0";

        // Act
        var client = _factory.CreateClientWithSession(baseUrl, sessionCookie, userAgent);

        // Assert
        Assert.NotNull(client);
        Assert.True(client.DefaultRequestHeaders.Contains("User-Agent"));
        var userAgentValues = client.DefaultRequestHeaders.GetValues("User-Agent");
        Assert.Contains(userAgent, userAgentValues);
    }

    [Fact]
    public void CreateRetryPolicy_ReturnsValidPolicy()
    {
        // Act
        var policy = RedmineHttpClientFactory.CreateRetryPolicy(_logger);

        // Assert
        Assert.NotNull(policy);
    }

    [Fact]
    public void CreateClient_WithoutLogger_WorksCorrectly()
    {
        // Arrange
        var factoryWithoutLogger = new RedmineHttpClientFactory(null);

        // Act
        var client = factoryWithoutLogger.CreateClient("https://redmine.example.com");

        // Assert
        Assert.NotNull(client);
        Assert.NotNull(client.BaseAddress);
    }

    [Fact]
    public void CreateClient_TrimsTrailingSlashFromBaseUrl()
    {
        // Arrange
        var baseUrl = "https://redmine.example.com///";

        // Act
        var client = _factory.CreateClient(baseUrl);

        // Assert
        Assert.NotNull(client);
        Assert.NotNull(client.BaseAddress);
        Assert.Equal("https://redmine.example.com/", client.BaseAddress.ToString());
    }

    [Fact]
    public void CreateClient_SetsJsonAcceptHeader()
    {
        // Act
        var client = _factory.CreateClient();

        // Assert
        Assert.NotNull(client);
        Assert.Contains(client.DefaultRequestHeaders.Accept, h => h.MediaType == "application/json");
    }
}
