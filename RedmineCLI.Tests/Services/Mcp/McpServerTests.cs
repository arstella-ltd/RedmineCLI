using System.Text.Json;

using FluentAssertions;

using Microsoft.Extensions.Logging;

using NSubstitute;

using RedmineCLI.Models;
using RedmineCLI.Models.Mcp;
using RedmineCLI.Services;
using RedmineCLI.Services.Mcp;

using Xunit;

namespace RedmineCLI.Tests.Services.Mcp;

public class McpServerTests
{
    private readonly IRedmineService _mockRedmineService;
    private readonly ILogger<McpServer> _mockLogger;
    private readonly McpServer _sut;

    public McpServerTests()
    {
        _mockRedmineService = Substitute.For<IRedmineService>();
        _mockLogger = Substitute.For<ILogger<McpServer>>();
        _sut = new McpServer(_mockRedmineService, _mockLogger);
    }

    #region Initialize Tests

    [Fact]
    public async Task HandleRequest_Should_ReturnServerInfo_When_InitializeMethod()
    {
        // Arrange
        var request = new JsonRpcRequest
        {
            JsonRpc = "2.0",
            Id = "init-1",
            Method = "initialize",
            Params = new { protocolVersion = "2024-11-05" }
        };

        // Act
        var response = await _sut.HandleRequestAsync(request);

        // Assert
        response.Should().NotBeNull();
        response.JsonRpc.Should().Be("2.0");
        response.Id.Should().Be("init-1");
        response.Result.Should().NotBeNull();
        response.Error.Should().BeNull();
    }

    #endregion

    #region Tools/List Tests

    [Fact]
    public async Task HandleRequest_Should_ReturnToolsList_When_ToolsListMethod()
    {
        // Arrange
        var request = new JsonRpcRequest
        {
            JsonRpc = "2.0",
            Id = "tools-1",
            Method = "tools/list"
        };

        // Act
        var response = await _sut.HandleRequestAsync(request);

        // Assert
        response.Should().NotBeNull();
        response.JsonRpc.Should().Be("2.0");
        response.Id.Should().Be("tools-1");
        response.Result.Should().NotBeNull();
        response.Error.Should().BeNull();

        // Verify tools list contains expected tools
        var json = JsonSerializer.Serialize(response.Result);
        json.Should().Contain("get_issues");
        json.Should().Contain("get_issue");
        json.Should().Contain("create_issue");
        json.Should().Contain("update_issue");
        json.Should().Contain("add_comment");
        json.Should().Contain("get_projects");
        json.Should().Contain("get_users");
        json.Should().Contain("get_statuses");
        json.Should().Contain("search");
    }

    #endregion

    #region Tools/Call Tests

    [Fact]
    public async Task HandleRequest_Should_GetIssues_When_ToolsCallWithGetIssues()
    {
        // Arrange
        var issues = new List<Issue>
        {
            new() { Id = 1, Subject = "Test Issue 1" },
            new() { Id = 2, Subject = "Test Issue 2" }
        };
        _mockRedmineService.GetIssuesAsync(Arg.Any<IssueFilter>(), Arg.Any<CancellationToken>())
            .Returns(issues);

        var request = new JsonRpcRequest
        {
            JsonRpc = "2.0",
            Id = "call-1",
            Method = "tools/call",
            Params = new { name = "get_issues", arguments = new { } }
        };

        // Act
        var response = await _sut.HandleRequestAsync(request);

        // Assert
        response.Should().NotBeNull();
        response.Error.Should().BeNull();
        response.Result.Should().NotBeNull();
    }

    [Fact]
    public async Task HandleRequest_Should_GetIssue_When_ToolsCallWithGetIssue()
    {
        // Arrange
        var issue = new Issue { Id = 123, Subject = "Test Issue" };
        _mockRedmineService.GetIssueAsync(123, Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(issue);

        var request = new JsonRpcRequest
        {
            JsonRpc = "2.0",
            Id = "call-2",
            Method = "tools/call",
            Params = new { name = "get_issue", arguments = new { issueId = 123 } }
        };

        // Act
        var response = await _sut.HandleRequestAsync(request);

        // Assert
        response.Should().NotBeNull();
        response.Error.Should().BeNull();
        response.Result.Should().NotBeNull();
    }

    [Fact]
    public async Task HandleRequest_Should_CreateIssue_When_ToolsCallWithCreateIssue()
    {
        // Arrange
        var createdIssue = new Issue { Id = 456, Subject = "New Issue", Project = new Project { Id = 1, Name = "Test Project" } };
        _mockRedmineService.CreateIssueAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(createdIssue);

        var request = new JsonRpcRequest
        {
            JsonRpc = "2.0",
            Id = "call-3",
            Method = "tools/call",
            Params = new { name = "create_issue", arguments = new { project = "test-project", subject = "New Issue" } }
        };

        // Act
        var response = await _sut.HandleRequestAsync(request);

        // Assert
        response.Should().NotBeNull();
        response.Error.Should().BeNull();
        response.Result.Should().NotBeNull();
        await _mockRedmineService.Received(1).CreateIssueAsync("test-project", "New Issue", Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleRequest_Should_UpdateIssue_When_ToolsCallWithUpdateIssue()
    {
        // Arrange
        var updatedIssue = new Issue { Id = 789, Subject = "Updated Subject" };
        _mockRedmineService.UpdateIssueAsync(Arg.Any<int>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<int?>(), Arg.Any<CancellationToken>())
            .Returns(updatedIssue);

        var request = new JsonRpcRequest
        {
            JsonRpc = "2.0",
            Id = "call-4",
            Method = "tools/call",
            Params = new { name = "update_issue", arguments = new { issueId = 789, subject = "Updated Subject" } }
        };

        // Act
        var response = await _sut.HandleRequestAsync(request);

        // Assert
        response.Should().NotBeNull();
        response.Error.Should().BeNull();
        response.Result.Should().NotBeNull();
        await _mockRedmineService.Received(1).UpdateIssueAsync(789, Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<int?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleRequest_Should_AddComment_When_ToolsCallWithAddComment()
    {
        // Arrange
        var request = new JsonRpcRequest
        {
            JsonRpc = "2.0",
            Id = "call-5",
            Method = "tools/call",
            Params = new { name = "add_comment", arguments = new { issueId = 999, comment = "Test comment" } }
        };

        // Act
        var response = await _sut.HandleRequestAsync(request);

        // Assert
        response.Should().NotBeNull();
        response.Error.Should().BeNull();
        response.Result.Should().NotBeNull();
        await _mockRedmineService.Received(1).AddCommentAsync(999, "Test comment", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleRequest_Should_GetProjects_When_ToolsCallWithGetProjects()
    {
        // Arrange
        var projects = new List<Project>
        {
            new() { Id = 1, Name = "Project 1" },
            new() { Id = 2, Name = "Project 2" }
        };
        _mockRedmineService.GetProjectsAsync(Arg.Any<CancellationToken>())
            .Returns(projects);

        var request = new JsonRpcRequest
        {
            JsonRpc = "2.0",
            Id = "call-6",
            Method = "tools/call",
            Params = new { name = "get_projects", arguments = new { } }
        };

        // Act
        var response = await _sut.HandleRequestAsync(request);

        // Assert
        response.Should().NotBeNull();
        response.Error.Should().BeNull();
        response.Result.Should().NotBeNull();
    }

    [Fact]
    public async Task HandleRequest_Should_GetUsers_When_ToolsCallWithGetUsers()
    {
        // Arrange
        var users = new List<User>
        {
            new() { Id = 1, Name = "User 1" },
            new() { Id = 2, Name = "User 2" }
        };
        _mockRedmineService.GetUsersAsync(Arg.Any<int?>(), Arg.Any<CancellationToken>())
            .Returns(users);

        var request = new JsonRpcRequest
        {
            JsonRpc = "2.0",
            Id = "call-7",
            Method = "tools/call",
            Params = new { name = "get_users", arguments = new { } }
        };

        // Act
        var response = await _sut.HandleRequestAsync(request);

        // Assert
        response.Should().NotBeNull();
        response.Error.Should().BeNull();
        response.Result.Should().NotBeNull();
    }

    [Fact]
    public async Task HandleRequest_Should_GetStatuses_When_ToolsCallWithGetStatuses()
    {
        // Arrange
        var statuses = new List<IssueStatus>
        {
            new() { Id = 1, Name = "New" },
            new() { Id = 2, Name = "In Progress" }
        };
        _mockRedmineService.GetIssueStatusesAsync(Arg.Any<CancellationToken>())
            .Returns(statuses);

        var request = new JsonRpcRequest
        {
            JsonRpc = "2.0",
            Id = "call-8",
            Method = "tools/call",
            Params = new { name = "get_statuses", arguments = new { } }
        };

        // Act
        var response = await _sut.HandleRequestAsync(request);

        // Assert
        response.Should().NotBeNull();
        response.Error.Should().BeNull();
        response.Result.Should().NotBeNull();
    }

    [Fact]
    public async Task HandleRequest_Should_Search_When_ToolsCallWithSearch()
    {
        // Arrange
        var searchResults = new List<Issue>
        {
            new() { Id = 1, Subject = "Result 1" },
            new() { Id = 2, Subject = "Result 2" }
        };
        _mockRedmineService.SearchIssuesAsync(Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<int?>(), Arg.Any<int?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(searchResults);

        var request = new JsonRpcRequest
        {
            JsonRpc = "2.0",
            Id = "call-9",
            Method = "tools/call",
            Params = new { name = "search", arguments = new { query = "test" } }
        };

        // Act
        var response = await _sut.HandleRequestAsync(request);

        // Assert
        response.Should().NotBeNull();
        response.Error.Should().BeNull();
        response.Result.Should().NotBeNull();
    }

    [Fact]
    public async Task HandleRequest_Should_ReturnError_When_ToolsCallWithUnknownTool()
    {
        // Arrange
        var request = new JsonRpcRequest
        {
            JsonRpc = "2.0",
            Id = "call-3",
            Method = "tools/call",
            Params = new { name = "unknown_tool", arguments = new { } }
        };

        // Act
        var response = await _sut.HandleRequestAsync(request);

        // Assert
        response.Should().NotBeNull();
        response.Error.Should().NotBeNull();
        response.Error!.Code.Should().Be(JsonRpcError.MethodNotFound);
    }

    #endregion

    #region Resources/List Tests

    [Fact]
    public async Task HandleRequest_Should_ReturnResourcesList_When_ResourcesListMethod()
    {
        // Arrange
        var request = new JsonRpcRequest
        {
            JsonRpc = "2.0",
            Id = "res-1",
            Method = "resources/list"
        };

        // Act
        var response = await _sut.HandleRequestAsync(request);

        // Assert
        response.Should().NotBeNull();
        response.JsonRpc.Should().Be("2.0");
        response.Id.Should().Be("res-1");
        response.Result.Should().NotBeNull();
        response.Error.Should().BeNull();
    }

    #endregion

    #region Resources/Read Tests

    [Fact]
    public async Task HandleRequest_Should_ReadIssueResource_When_ResourcesReadWithIssueUri()
    {
        // Arrange
        var issue = new Issue { Id = 456, Subject = "Test Issue" };
        _mockRedmineService.GetIssueAsync(456, Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(issue);

        var request = new JsonRpcRequest
        {
            JsonRpc = "2.0",
            Id = "read-1",
            Method = "resources/read",
            Params = new { uri = "issue://456" }
        };

        // Act
        var response = await _sut.HandleRequestAsync(request);

        // Assert
        response.Should().NotBeNull();
        response.Error.Should().BeNull();
        response.Result.Should().NotBeNull();
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public async Task HandleRequest_Should_ReturnError_When_MethodNotFound()
    {
        // Arrange
        var request = new JsonRpcRequest
        {
            JsonRpc = "2.0",
            Id = "err-1",
            Method = "unknown/method"
        };

        // Act
        var response = await _sut.HandleRequestAsync(request);

        // Assert
        response.Should().NotBeNull();
        response.Error.Should().NotBeNull();
        response.Error!.Code.Should().Be(JsonRpcError.MethodNotFound);
    }

    #endregion
}
