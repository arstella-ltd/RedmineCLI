using FluentAssertions;

using RedmineCLI.Commands;

using Xunit;

namespace RedmineCLI.Tests.Commands;

public class IssueListOptionsTests
{
    [Fact]
    public void Constructor_Should_InitializePropertiesWithDefaultValues()
    {
        // Act
        var options = new IssueListOptions();

        // Assert
        options.Assignee.Should().BeNull();
        options.Status.Should().BeNull();
        options.Project.Should().BeNull();
        options.Limit.Should().BeNull();
        options.Offset.Should().BeNull();
        options.Json.Should().BeFalse();
        options.Web.Should().BeFalse();
        options.AbsoluteTime.Should().BeFalse();
        options.Search.Should().BeNull();
        options.Sort.Should().BeNull();
        options.Priority.Should().BeNull();
        options.Author.Should().BeNull();
    }

    [Fact]
    public void Properties_Should_BeSettable()
    {
        // Arrange
        var options = new IssueListOptions();

        // Act
        options.Assignee = "john.doe";
        options.Status = "open";
        options.Project = "test-project";
        options.Limit = 50;
        options.Offset = 10;
        options.Json = true;
        options.Web = true;
        options.AbsoluteTime = true;
        options.Search = "bug";
        options.Sort = "priority:desc";
        options.Priority = "high";
        options.Author = "jane.smith";

        // Assert
        options.Assignee.Should().Be("john.doe");
        options.Status.Should().Be("open");
        options.Project.Should().Be("test-project");
        options.Limit.Should().Be(50);
        options.Offset.Should().Be(10);
        options.Json.Should().BeTrue();
        options.Web.Should().BeTrue();
        options.AbsoluteTime.Should().BeTrue();
        options.Search.Should().Be("bug");
        options.Sort.Should().Be("priority:desc");
        options.Priority.Should().Be("high");
        options.Author.Should().Be("jane.smith");
    }

    [Fact]
    public void Constructor_Should_AcceptAllParameters()
    {
        // Act
        var options = new IssueListOptions
        {
            Assignee = "@me",
            Status = "all",
            Project = "my-project",
            Limit = 100,
            Offset = 20,
            Json = true,
            Web = false,
            AbsoluteTime = true,
            Search = "keyword",
            Sort = "updated_on:desc",
            Priority = "urgent",
            Author = "@me"
        };

        // Assert
        options.Assignee.Should().Be("@me");
        options.Status.Should().Be("all");
        options.Project.Should().Be("my-project");
        options.Limit.Should().Be(100);
        options.Offset.Should().Be(20);
        options.Json.Should().BeTrue();
        options.Web.Should().BeFalse();
        options.AbsoluteTime.Should().BeTrue();
        options.Search.Should().Be("keyword");
        options.Sort.Should().Be("updated_on:desc");
        options.Priority.Should().Be("urgent");
        options.Author.Should().Be("@me");
    }
}
