using System.Text.Json;

using FluentAssertions;

using RedmineCLI.ApiClient;
using RedmineCLI.Models;

namespace RedmineCLI.Tests.ApiClient;

public class SerializationTests
{
    private readonly RedmineJsonContext _jsonContext;

    public SerializationTests()
    {
        _jsonContext = new RedmineJsonContext(new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            WriteIndented = true,
            Converters = { new DateTimeConverter() }
        });
    }

    [Fact]
    public void Serialize_Should_ProduceValidJson_When_ModelIsValid()
    {
        // Arrange
        var issue = new Issue
        {
            Id = 123,
            Subject = "Test Issue",
            Description = "Test Description",
            Project = new Project { Id = 1, Name = "Test Project" },
            Status = new IssueStatus { Id = 2, Name = "In Progress" },
            Priority = new Priority { Id = 3, Name = "High" },
            AssignedTo = new User { Id = 10, Name = "John Doe" },
            CreatedOn = new DateTime(2024, 1, 15, 10, 30, 0, DateTimeKind.Utc),
            UpdatedOn = new DateTime(2024, 1, 16, 14, 45, 0, DateTimeKind.Utc),
            DoneRatio = 50
        };

        // Act
        var json = JsonSerializer.Serialize(issue, _jsonContext.Issue);

        // Assert
        json.Should().NotBeNullOrEmpty();
        json.Should().Contain("\"id\": 123");
        json.Should().Contain("\"subject\": \"Test Issue\"");
        json.Should().Contain("\"description\": \"Test Description\"");
        json.Should().Contain("\"done_ratio\": 50");
        json.Should().Contain("\"project\":");
        json.Should().Contain("\"status\":");
        json.Should().Contain("\"priority\":");
        json.Should().Contain("\"assigned_to\":");
    }

    [Fact]
    public void Deserialize_Should_CreateObject_When_JsonIsValid()
    {
        // Arrange
        var json = @"{
            ""id"": 456,
            ""subject"": ""Deserialized Issue"",
            ""description"": ""This is a test"",
            ""project"": {
                ""id"": 2,
                ""name"": ""Project B""
            },
            ""status"": {
                ""id"": 1,
                ""name"": ""New""
            },
            ""priority"": {
                ""id"": 2,
                ""name"": ""Normal""
            },
            ""assigned_to"": {
                ""id"": 20,
                ""name"": ""Jane Smith""
            },
            ""created_on"": ""2024-01-10T08:00:00Z"",
            ""updated_on"": ""2024-01-11T16:30:00Z"",
            ""done_ratio"": 75
        }";

        // Act
        var issue = JsonSerializer.Deserialize(json, _jsonContext.Issue);

        // Assert
        issue.Should().NotBeNull();
        issue!.Id.Should().Be(456);
        issue.Subject.Should().Be("Deserialized Issue");
        issue.Description.Should().Be("This is a test");
        issue.Project.Should().NotBeNull();
        issue.Project!.Id.Should().Be(2);
        issue.Project.Name.Should().Be("Project B");
        issue.Status.Should().NotBeNull();
        issue.Status!.Id.Should().Be(1);
        issue.Status.Name.Should().Be("New");
        issue.Priority.Should().NotBeNull();
        issue.Priority!.Id.Should().Be(2);
        issue.Priority.Name.Should().Be("Normal");
        issue.AssignedTo.Should().NotBeNull();
        issue.AssignedTo!.Id.Should().Be(20);
        issue.AssignedTo.Name.Should().Be("Jane Smith");
        issue.DoneRatio.Should().Be(75);
    }

    [Fact]
    public void Serialize_Should_HandleNullableProperties_When_ValuesAreNull()
    {
        // Arrange
        var issue = new Issue
        {
            Id = 789,
            Subject = "Issue with nulls",
            Description = null,
            AssignedTo = null,
            DoneRatio = null
        };

        // Act
        var json = JsonSerializer.Serialize(issue, _jsonContext.Issue);
        var deserialized = JsonSerializer.Deserialize(json, _jsonContext.Issue);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized!.Id.Should().Be(789);
        deserialized.Subject.Should().Be("Issue with nulls");
        deserialized.Description.Should().BeNull();
        deserialized.AssignedTo.Should().BeNull();
        deserialized.DoneRatio.Should().BeNull();
    }

    [Fact]
    public void Deserialize_Should_HandleApiResponseWrapper_When_JsonHasRootElement()
    {
        // Arrange
        var json = @"{
            ""issue"": {
                ""id"": 999,
                ""subject"": ""Wrapped Issue""
            }
        }";

        // Act
        var response = JsonSerializer.Deserialize(json, _jsonContext.IssueResponse);

        // Assert
        response.Should().NotBeNull();
        response!.Issue.Should().NotBeNull();
        response.Issue!.Id.Should().Be(999);
        response.Issue.Subject.Should().Be("Wrapped Issue");
    }

    [Fact]
    public void Deserialize_Should_HandleListResponse_When_JsonHasMultipleItems()
    {
        // Arrange
        var json = @"{
            ""issues"": [
                {
                    ""id"": 1,
                    ""subject"": ""Issue 1""
                },
                {
                    ""id"": 2,
                    ""subject"": ""Issue 2""
                }
            ],
            ""total_count"": 2,
            ""offset"": 0,
            ""limit"": 25
        }";

        // Act
        var response = JsonSerializer.Deserialize(json, _jsonContext.IssuesResponse);

        // Assert
        response.Should().NotBeNull();
        response!.Issues.Should().HaveCount(2);
        response.Issues[0].Id.Should().Be(1);
        response.Issues[0].Subject.Should().Be("Issue 1");
        response.Issues[1].Id.Should().Be(2);
        response.Issues[1].Subject.Should().Be("Issue 2");
        response.TotalCount.Should().Be(2);
        response.Offset.Should().Be(0);
        response.Limit.Should().Be(25);
    }
}
