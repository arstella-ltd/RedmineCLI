using FluentAssertions;

using RedmineCLI.ApiClient;
using RedmineCLI.Models;

using Xunit;

namespace RedmineCLI.Tests.Models;

public class ModelPropertyTests
{
    [Fact]
    public void AttachmentResponse_Should_SetAndGetProperties()
    {
        // Arrange & Act
        var response = new AttachmentResponse
        {
            Attachment = new Attachment { Id = 1, Filename = "test.pdf" }
        };

        // Assert
        response.Attachment.Should().NotBeNull();
        response.Attachment.Id.Should().Be(1);
        response.Attachment.Filename.Should().Be("test.pdf");
    }

    [Fact]
    public void SearchResponse_Should_SetAndGetProperties()
    {
        // Arrange & Act
        var response = new SearchResponse
        {
            Results = new List<SearchResult>
            {
                new SearchResult { Id = 1, Title = "Result 1" },
                new SearchResult { Id = 2, Title = "Result 2" }
            },
            TotalCount = 100,
            Offset = 0,
            Limit = 25
        };

        // Assert
        response.Results.Should().HaveCount(2);
        response.TotalCount.Should().Be(100);
        response.Offset.Should().Be(0);
        response.Limit.Should().Be(25);
    }

    [Fact]
    public void SearchResult_Should_SetAndGetAllProperties()
    {
        // Arrange & Act
        var result = new SearchResult
        {
            Id = 123,
            Title = "Test Issue Title",
            Type = "issue",
            Url = "https://example.com/issues/123",
            Description = "Test description",
            Datetime = new DateTime(2024, 1, 1, 10, 0, 0),
            Project = new Project { Id = 10, Name = "Test Project" }
        };

        // Assert
        result.Id.Should().Be(123);
        result.Title.Should().Be("Test Issue Title");
        result.Type.Should().Be("issue");
        result.Url.Should().Be("https://example.com/issues/123");
        result.Description.Should().Be("Test description");
        result.Datetime.Should().Be(new DateTime(2024, 1, 1, 10, 0, 0));
        result.Project.Should().NotBeNull();
        result.Project.Id.Should().Be(10);
    }

    [Fact]
    public void PrioritiesResponse_Should_SetAndGetProperties()
    {
        // Arrange & Act
        var response = new PrioritiesResponse
        {
            Priorities = new List<Priority>
            {
                new Priority { Id = 1, Name = "Low" },
                new Priority { Id = 2, Name = "Normal" },
                new Priority { Id = 3, Name = "High" }
            }
        };

        // Assert
        response.Priorities.Should().HaveCount(3);
        response.Priorities[0].Id.Should().Be(1);
        response.Priorities[0].Name.Should().Be("Low");
        response.Priorities[1].Id.Should().Be(2);
        response.Priorities[1].Name.Should().Be("Normal");
        response.Priorities[2].Id.Should().Be(3);
        response.Priorities[2].Name.Should().Be("High");
    }

    [Fact]
    public void TimeSettings_Should_SetAndGetFormat()
    {
        // Arrange & Act
        var settings = new TimeSettings
        {
            Format = "absolute"
        };

        // Assert
        settings.Format.Should().Be("absolute");
    }

    [Fact]
    public void User_DisplayName_Should_ReturnName_When_NameIsProvided()
    {
        // Arrange
        var user = new User
        {
            Id = 1,
            Login = "jdoe",
            Name = "John Doe",
            FirstName = "John",
            LastName = "Doe"
        };

        // Act & Assert
        user.DisplayName.Should().Be("John Doe");
    }

    [Fact]
    public void User_DisplayName_Should_BuildFromFirstAndLastName_When_NameIsNull()
    {
        // Arrange
        var user = new User
        {
            Id = 1,
            Login = "jdoe",
            Name = null,
            FirstName = "John",
            LastName = "Doe"
        };

        // Act & Assert
        user.DisplayName.Should().Be("John Doe");
    }

    [Fact]
    public void User_DisplayName_Should_ReturnLogin_When_NoNameProvided()
    {
        // Arrange
        var user = new User
        {
            Id = 1,
            Login = "jdoe",
            Name = null,
            FirstName = null,
            LastName = null
        };

        // Act & Assert
        user.DisplayName.Should().Be("jdoe");
    }

    [Fact]
    public void User_Mail_Should_ReturnEmail()
    {
        // Arrange
        var user = new User
        {
            Email = "test@example.com"
        };

        // Act & Assert
        user.Mail.Should().Be("test@example.com");
    }

    [Fact]
    public void IssueStatus_Should_SetAndGetProperties()
    {
        // Arrange & Act
        var status = new IssueStatus
        {
            Id = 1,
            Name = "New",
            IsClosed = false,
            IsDefault = true
        };

        // Assert
        status.Id.Should().Be(1);
        status.Name.Should().Be("New");
        status.IsClosed.Should().BeFalse();
        status.IsDefault.Should().BeTrue();
    }

    [Fact]
    public void Priority_Should_SetAndGetProperties()
    {
        // Arrange & Act
        var priority = new Priority
        {
            Id = 2,
            Name = "Normal"
        };

        // Assert
        priority.Id.Should().Be(2);
        priority.Name.Should().Be("Normal");
    }

    [Fact]
    public void Project_Should_SetAndGetAllProperties()
    {
        // Arrange & Act
        var project = new Project
        {
            Id = 10,
            Name = "Test Project",
            Identifier = "test-proj",
            Description = "Test project description",
            CreatedOn = new DateTime(2024, 1, 1, 10, 0, 0),
            UpdatedOn = new DateTime(2024, 1, 5, 15, 0, 0)
        };

        // Assert
        project.Id.Should().Be(10);
        project.Name.Should().Be("Test Project");
        project.Identifier.Should().Be("test-proj");
        project.Description.Should().Be("Test project description");
        project.CreatedOn.Should().Be(new DateTime(2024, 1, 1, 10, 0, 0));
        project.UpdatedOn.Should().Be(new DateTime(2024, 1, 5, 15, 0, 0));
    }

    [Fact]
    public void Attachment_Should_SetAndGetAllProperties()
    {
        // Arrange & Act
        var attachment = new Attachment
        {
            Id = 100,
            Filename = "document.pdf",
            Filesize = 1024,
            ContentType = "application/pdf",
            Description = "Test document",
            ContentUrl = "https://example.com/attachments/100",
            Author = new User { Id = 10, Name = "Author" },
            CreatedOn = new DateTime(2024, 1, 1, 10, 0, 0)
        };

        // Assert
        attachment.Id.Should().Be(100);
        attachment.Filename.Should().Be("document.pdf");
        attachment.Filesize.Should().Be(1024);
        attachment.ContentType.Should().Be("application/pdf");
        attachment.Description.Should().Be("Test document");
        attachment.ContentUrl.Should().Be("https://example.com/attachments/100");
        attachment.Author.Should().NotBeNull();
        attachment.Author.Id.Should().Be(10);
        attachment.CreatedOn.Should().Be(new DateTime(2024, 1, 1, 10, 0, 0));
    }
}
