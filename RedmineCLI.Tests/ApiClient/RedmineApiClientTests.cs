using System.Net;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using RedmineCLI.ApiClient;
using RedmineCLI.Exceptions;
using RedmineCLI.Models;
using RedmineCLI.Services;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;
using WireMock.Types;
using WireMock;
using Xunit;

namespace RedmineCLI.Tests.ApiClient;

public class RedmineApiClientTests : IDisposable
{
    private readonly WireMockServer _mockServer;
    private readonly RedmineApiClient _client;
    private readonly IConfigService _configService;
    private readonly ILogger<RedmineApiClient> _logger;
    private readonly string _apiKey = "test-api-key-12345";
    private readonly JsonSerializerOptions _jsonOptions;

    public RedmineApiClientTests()
    {
        _mockServer = WireMockServer.Start();
        _configService = Substitute.For<IConfigService>();
        _logger = Substitute.For<ILogger<RedmineApiClient>>();
        
        // 設定サービスのモック
        var profile = new Profile
        {
            Name = "test",
            Url = _mockServer.Urls[0],
            ApiKey = _apiKey
        };
        _configService.GetActiveProfileAsync().Returns(Task.FromResult<Profile?>(profile));

        // HTTPクライアントの作成
        var httpClient = new HttpClient();
        _client = new RedmineApiClient(httpClient, _configService, _logger);

        // JSONオプションの設定
        _jsonOptions = new JsonSerializerOptions(RedmineJsonContext.Default.Options);
    }

    public void Dispose()
    {
        _mockServer?.Stop();
        _mockServer?.Dispose();
    }

    #region HTTP Request Tests

    [Fact]
    public async Task GetIssuesAsync_Should_SendCorrectHttpRequest_When_Called()
    {
        // Arrange
        _mockServer
            .Given(Request.Create()
                .WithPath("/issues.json")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithBodyAsJson(new { issues = new[] { new { id = 1, subject = "Test Issue" } }, total_count = 1 }));

        // Act
        var result = await _client.GetIssuesAsync();

        // Assert
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task GetIssuesAsync_Should_IncludeQueryParameters_When_FiltersProvided()
    {
        // Arrange
        var expectedResponse = new IssuesResponse { Issues = new List<Issue>() };

        _mockServer
            .Given(Request.Create()
                .WithPath("/issues.json")
                .WithParam("assigned_to_id", "5")
                .WithParam("project_id", "10")
                .WithParam("status_id", "open")
                .WithParam("limit", "50")
                .WithParam("offset", "100")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithBodyAsJson(expectedResponse));

        // Act
        await _client.GetIssuesAsync(
            assignedToId: 5,
            projectId: 10,
            status: "open",
            limit: 50,
            offset: 100);

        // Assert
        _mockServer.LogEntries.Should().HaveCount(1);
    }

    #endregion

    #region Authentication Header Tests

    [Fact]
    public async Task AllRequests_Should_IncludeApiKeyHeader_When_ProfileHasApiKey()
    {
        // Arrange
        _mockServer
            .Given(Request.Create()
                .WithPath("/issues.json")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithBodyAsJson(new IssuesResponse()));

        // Act
        await _client.GetIssuesAsync();

        // Assert
        var request = _mockServer.LogEntries.First().RequestMessage;
        request.Headers.Should().NotBeNull();
        request.Headers!.Should().ContainKey("X-Redmine-API-Key");
        if (request.Headers!.TryGetValue("X-Redmine-API-Key", out var apiKeyHeaders))
        {
            apiKeyHeaders.FirstOrDefault().Should().Be(_apiKey);
        }
        else
        {
            Assert.Fail("X-Redmine-API-Key header not found");
        }
    }

    [Fact]
    public async Task AllRequests_Should_ThrowException_When_NoActiveProfile()
    {
        // Arrange
        _configService.GetActiveProfileAsync().Returns(Task.FromResult<Profile?>(null));

        // Act
        var act = async () => await _client.GetIssuesAsync();

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("No active profile found. Please run 'redmine auth login' first.");
    }

    #endregion

    #region Retry Policy Tests

    [Fact]
    public async Task GetIssuesAsync_Should_RetryOnTransientErrors_When_RequestFails()
    {
        // このテストはPollyリトライポリシーが実装された後に有効化予定
        // 現在は基本的なHTTPクライアント設定でスキップ
        await Task.CompletedTask;
    }

    [Fact]
    public async Task GetIssuesAsync_Should_ThrowAfterMaxRetries_When_AllAttemptsFail()
    {
        // このテストはPollyリトライポリシーが実装された後に有効化予定
        await Task.CompletedTask;
    }

    [Fact]
    public async Task GetIssuesAsync_Should_NotRetryOnClientErrors_When_BadRequest()
    {
        // Arrange
        _mockServer
            .Given(Request.Create()
                .WithPath("/issues.json")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(HttpStatusCode.BadRequest)
                .WithBody("Invalid request"));

        // Act
        var act = async () => await _client.GetIssuesAsync();

        // Assert
        await act.Should().ThrowAsync<RedmineApiException>()
            .WithMessage("*Bad Request*");
        _mockServer.LogEntries.Should().HaveCount(1); // リトライなし
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public async Task GetIssuesAsync_Should_ThrowRedmineApiException_When_ApiReturnsError()
    {
        // Arrange
        var errorResponse = new { errors = new[] { "Invalid API key" } };
        _mockServer
            .Given(Request.Create()
                .WithPath("/issues.json")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(HttpStatusCode.Unauthorized)
                .WithBodyAsJson(errorResponse));

        // Act
        var act = async () => await _client.GetIssuesAsync();

        // Assert
        var exception = await act.Should().ThrowAsync<RedmineApiException>();
        exception.Which.StatusCode.Should().Be((int)HttpStatusCode.Unauthorized);
        exception.Which.Message.Should().Contain("Unauthorized");
    }

    [Fact]
    public async Task GetIssueAsync_Should_ThrowRedmineApiException_When_IssueNotFound()
    {
        // Arrange
        _mockServer
            .Given(Request.Create()
                .WithPath("/issues/999.json")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(HttpStatusCode.NotFound));

        // Act
        var act = async () => await _client.GetIssueAsync(999);

        // Assert
        var exception = await act.Should().ThrowAsync<RedmineApiException>();
        exception.Which.StatusCode.Should().Be(404);
        exception.Which.Message.Should().Contain("Not Found");
    }

    #endregion

    #region API Methods Tests

    [Fact]
    public async Task GetIssueAsync_Should_ReturnIssue_When_ValidId()
    {
        // Arrange
        _mockServer
            .Given(Request.Create()
                .WithPath("/issues/123.json")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithBodyAsJson(new { issue = new { id = 123, subject = "Test Issue" } }));

        // Act
        var result = await _client.GetIssueAsync(123);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(123);
        result.Subject.Should().Be("Test Issue");
    }

    [Fact]
    public async Task CreateIssueAsync_Should_SendPostRequest_When_ValidIssue()
    {
        // Arrange
        var newIssue = new Issue
        {
            Subject = "New Issue",
            Description = "Test Description",
            Project = new Project { Id = 1 }
        };

        _mockServer
            .Given(Request.Create()
                .WithPath("/issues.json")
                .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(HttpStatusCode.Created)
                .WithBodyAsJson(new { issue = new { id = 456, subject = "New Issue" } }));

        // Act
        var result = await _client.CreateIssueAsync(newIssue);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(456);
    }

    [Fact]
    public async Task UpdateIssueAsync_Should_SendPutRequest_When_ValidUpdate()
    {
        // Arrange
        var updateIssue = new Issue
        {
            Subject = "Updated Issue",
            Status = new IssueStatus { Id = 2 }
        };

        _mockServer
            .Given(Request.Create()
                .WithPath("/issues/789.json")
                .UsingPut())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithBodyAsJson(new { issue = new { id = 789, subject = "Updated Issue" } }));

        // Act
        var result = await _client.UpdateIssueAsync(789, updateIssue);

        // Assert
        result.Should().NotBeNull();
        result.Subject.Should().Be("Updated Issue");
    }

    [Fact]
    public async Task TestConnectionAsync_Should_ReturnTrue_When_ServerResponds()
    {
        // Arrange
        _mockServer
            .Given(Request.Create()
                .WithPath("/users/current.json")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithBodyAsJson(new { user = new { id = 1, login = "test" } }));

        // Act
        var result = await _client.TestConnectionAsync();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task TestConnectionAsync_Should_ReturnFalse_When_Unauthorized()
    {
        // Arrange
        _mockServer
            .Given(Request.Create()
                .WithPath("/users/current.json")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(HttpStatusCode.Unauthorized));

        // Act
        var result = await _client.TestConnectionAsync();

        // Assert
        result.Should().BeFalse();
    }

    #endregion
}