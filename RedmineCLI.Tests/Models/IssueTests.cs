using FluentAssertions;
using RedmineCLI.Models;

namespace RedmineCLI.Tests.Models;

public class IssueTests
{
    [Fact]
    public void Constructor_Should_InitializeProperties_When_ValidDataProvided()
    {
        // Arrange
        var id = 123;
        var subject = "Test Issue";
        var description = "Test Description";
        var projectId = 1;
        var projectName = "Test Project";
        var statusId = 2;
        var statusName = "In Progress";
        var priority = "High";
        var assigneeId = 10;
        var assigneeName = "John Doe";
        var createdOn = DateTime.UtcNow.AddDays(-1);
        var updatedOn = DateTime.UtcNow;

        // Act
        var issue = new Issue
        {
            Id = id,
            Subject = subject,
            Description = description,
            Project = new Project { Id = projectId, Name = projectName },
            Status = new IssueStatus { Id = statusId, Name = statusName },
            Priority = new Priority { Name = priority },
            AssignedTo = new User { Id = assigneeId, Name = assigneeName },
            CreatedOn = createdOn,
            UpdatedOn = updatedOn
        };

        // Assert
        issue.Id.Should().Be(id);
        issue.Subject.Should().Be(subject);
        issue.Description.Should().Be(description);
        issue.Project.Should().NotBeNull();
        issue.Project.Id.Should().Be(projectId);
        issue.Project.Name.Should().Be(projectName);
        issue.Status.Should().NotBeNull();
        issue.Status.Id.Should().Be(statusId);
        issue.Status.Name.Should().Be(statusName);
        issue.Priority.Should().NotBeNull();
        issue.Priority.Name.Should().Be(priority);
        issue.AssignedTo.Should().NotBeNull();
        issue.AssignedTo.Id.Should().Be(assigneeId);
        issue.AssignedTo.Name.Should().Be(assigneeName);
        issue.CreatedOn.Should().Be(createdOn);
        issue.UpdatedOn.Should().Be(updatedOn);
    }

    [Fact]
    public void Validation_Should_ThrowException_When_RequiredFieldsAreMissing()
    {
        // Arrange
        var issue = new Issue();

        // Act
        var act = () => issue.Validate();

        // Assert
        act.Should().Throw<ValidationException>()
            .WithMessage("Subject is required");
    }

    [Fact]
    public void Equals_Should_ReturnTrue_When_IdsAreEqual()
    {
        // Arrange
        var issue1 = new Issue { Id = 123, Subject = "Issue 1" };
        var issue2 = new Issue { Id = 123, Subject = "Issue 2" };

        // Act
        var result = issue1.Equals(issue2);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void Equals_Should_ReturnFalse_When_IdsAreDifferent()
    {
        // Arrange
        var issue1 = new Issue { Id = 123, Subject = "Issue 1" };
        var issue2 = new Issue { Id = 456, Subject = "Issue 1" };

        // Act
        var result = issue1.Equals(issue2);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void GetHashCode_Should_ReturnSameValue_When_IdsAreEqual()
    {
        // Arrange
        var issue1 = new Issue { Id = 123, Subject = "Issue 1" };
        var issue2 = new Issue { Id = 123, Subject = "Issue 2" };

        // Act
        var hash1 = issue1.GetHashCode();
        var hash2 = issue2.GetHashCode();

        // Assert
        hash1.Should().Be(hash2);
    }
}