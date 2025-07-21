using System.Text.Json;

using FluentAssertions;

using RedmineCLI.ApiClient;
using RedmineCLI.Models;

using Xunit;

namespace RedmineCLI.Tests.ApiClient;

public class ApiResponsesTests
{
    private readonly JsonSerializerOptions _jsonOptions;

    public ApiResponsesTests()
    {
        _jsonOptions = RedmineJsonContext.Default.Options;
    }

    [Fact]
    public void ProjectsResponse_Should_DeserializeCorrectly()
    {
        // Arrange
        var json = @"{
            ""projects"": [
                { ""id"": 1, ""name"": ""Project 1"", ""identifier"": ""project-1"" },
                { ""id"": 2, ""name"": ""Project 2"", ""identifier"": ""project-2"" }
            ],
            ""total_count"": 2,
            ""offset"": 0,
            ""limit"": 25
        }";

        // Act
        var response = JsonSerializer.Deserialize<ProjectsResponse>(json, _jsonOptions);

        // Assert
        response.Should().NotBeNull();
        response!.Projects.Should().HaveCount(2);
        response.Projects[0].Id.Should().Be(1);
        response.Projects[0].Name.Should().Be("Project 1");
        response.Projects[1].Id.Should().Be(2);
        response.Projects[1].Name.Should().Be("Project 2");
    }

    [Fact]
    public void UsersResponse_Should_DeserializeCorrectly()
    {
        // Arrange
        var json = @"{
            ""users"": [
                { ""id"": 10, ""login"": ""john.doe"", ""firstname"": ""John"", ""lastname"": ""Doe"" },
                { ""id"": 20, ""login"": ""jane.smith"", ""firstname"": ""Jane"", ""lastname"": ""Smith"" }
            ],
            ""total_count"": 2,
            ""offset"": 0,
            ""limit"": 25
        }";

        // Act
        var response = JsonSerializer.Deserialize<UsersResponse>(json, _jsonOptions);

        // Assert
        response.Should().NotBeNull();
        response!.Users.Should().HaveCount(2);
        response.Users[0].Id.Should().Be(10);
        response.Users[0].Login.Should().Be("john.doe");
        response.Users[0].FirstName.Should().Be("John");
        response.Users[0].LastName.Should().Be("Doe");
        response.Users[1].Id.Should().Be(20);
        response.Users[1].Login.Should().Be("jane.smith");
    }

    [Fact]
    public void IssueStatusesResponse_Should_DeserializeCorrectly()
    {
        // Arrange
        var json = @"{
            ""issue_statuses"": [
                { ""id"": 1, ""name"": ""New"", ""is_closed"": false },
                { ""id"": 2, ""name"": ""In Progress"", ""is_closed"": false },
                { ""id"": 3, ""name"": ""Closed"", ""is_closed"": true }
            ]
        }";

        // Act
        var response = JsonSerializer.Deserialize<IssueStatusesResponse>(json, _jsonOptions);

        // Assert
        response.Should().NotBeNull();
        response!.IssueStatuses.Should().HaveCount(3);
        response.IssueStatuses[0].Id.Should().Be(1);
        response.IssueStatuses[0].Name.Should().Be("New");
        response.IssueStatuses[0].IsClosed.Should().BeFalse();
        response.IssueStatuses[2].Id.Should().Be(3);
        response.IssueStatuses[2].Name.Should().Be("Closed");
        response.IssueStatuses[2].IsClosed.Should().BeTrue();
    }

    [Fact]
    public void UserResponse_Should_DeserializeCorrectly()
    {
        // Arrange
        var json = @"{
            ""user"": {
                ""id"": 100,
                ""login"": ""current.user"",
                ""firstname"": ""Current"",
                ""lastname"": ""User"",
                ""mail"": ""current@example.com""
            }
        }";

        // Act
        var response = JsonSerializer.Deserialize<UserResponse>(json, _jsonOptions);

        // Assert
        response.Should().NotBeNull();
        response!.User.Should().NotBeNull();
        response.User.Id.Should().Be(100);
        response.User.Login.Should().Be("current.user");
        response.User.FirstName.Should().Be("Current");
        response.User.LastName.Should().Be("User");
        response.User.Email.Should().Be("current@example.com");
    }

    [Fact]
    public void IssueRequest_Should_SerializeCorrectly()
    {
        // Arrange
        var issueRequest = new IssueRequest
        {
            Issue = new Issue
            {
                Subject = "Test Issue",
                Description = "Test Description",
                Project = new Project { Id = 5 },
                Status = new IssueStatus { Id = 1 },
                Priority = new Priority { Id = 2 },
                AssignedTo = new User { Id = 10 }
            }
        };

        // Act
        var json = JsonSerializer.Serialize(issueRequest, _jsonOptions);

        // Assert
        json.Should().NotBeNullOrEmpty();
        json.Should().Contain("Test Issue");
        json.Should().Contain("Test Description");
        json.Should().Contain("\"id\": 5");
        json.Should().Contain("\"status\":");
        json.Should().Contain("\"priority\":");
        json.Should().Contain("\"assigned_to\":");
    }

    [Fact]
    public void CommentRequest_Should_SerializeCorrectly()
    {
        // Arrange
        var commentRequest = new CommentRequest
        {
            Issue = new CommentData
            {
                Notes = "This is a test comment"
            }
        };

        // Act
        var json = JsonSerializer.Serialize(commentRequest, _jsonOptions);

        // Assert
        json.Should().NotBeNullOrEmpty();
        json.Should().Contain("\"issue\":");
        json.Should().Contain("This is a test comment");
    }

    [Fact]
    public void ProjectsResponse_Should_HandleEmptyArray()
    {
        // Arrange
        var json = @"{
            ""projects"": [],
            ""total_count"": 0,
            ""offset"": 0,
            ""limit"": 25
        }";

        // Act
        var response = JsonSerializer.Deserialize<ProjectsResponse>(json, _jsonOptions);

        // Assert
        response.Should().NotBeNull();
        response!.Projects.Should().BeEmpty();
    }

    [Fact]
    public void UsersResponse_Should_HandleEmptyArray()
    {
        // Arrange
        var json = @"{
            ""users"": [],
            ""total_count"": 0,
            ""offset"": 0,
            ""limit"": 25
        }";

        // Act
        var response = JsonSerializer.Deserialize<UsersResponse>(json, _jsonOptions);

        // Assert
        response.Should().NotBeNull();
        response!.Users.Should().BeEmpty();
    }

    [Fact]
    public void IssueStatusesResponse_Should_HandleEmptyArray()
    {
        // Arrange
        var json = @"{
            ""issue_statuses"": []
        }";

        // Act
        var response = JsonSerializer.Deserialize<IssueStatusesResponse>(json, _jsonOptions);

        // Assert
        response.Should().NotBeNull();
        response!.IssueStatuses.Should().BeEmpty();
    }
}
