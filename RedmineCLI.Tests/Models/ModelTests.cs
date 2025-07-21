using FluentAssertions;

using RedmineCLI.Exceptions;
using RedmineCLI.Models;

using Xunit;

namespace RedmineCLI.Tests.Models;

public class ModelTests
{
    #region Issue Tests

    [Fact]
    public void Issue_Validate_Should_ThrowException_When_SubjectIsEmpty()
    {
        // Arrange
        var issue = new Issue
        {
            Id = 1,
            Subject = "",
            Description = "Test"
        };

        // Act
        var act = () => issue.Validate();

        // Assert
        act.Should().Throw<ValidationException>()
            .WithMessage("Subject is required");
    }

    [Fact]
    public void Issue_Validate_Should_ThrowException_When_SubjectIsNull()
    {
        // Arrange
        var issue = new Issue
        {
            Id = 1,
            Subject = null!,
            Description = "Test"
        };

        // Act
        var act = () => issue.Validate();

        // Assert
        act.Should().Throw<ValidationException>()
            .WithMessage("Subject is required");
    }

    [Fact]
    public void Issue_Validate_Should_NotThrow_When_SubjectIsValid()
    {
        // Arrange
        var issue = new Issue
        {
            Id = 1,
            Subject = "Valid Subject",
            Description = "Test"
        };

        // Act
        var act = () => issue.Validate();

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void Issue_Equals_Should_ReturnTrue_When_SameId()
    {
        // Arrange
        var issue1 = new Issue { Id = 123, Subject = "Issue 1" };
        var issue2 = new Issue { Id = 123, Subject = "Different Subject" };

        // Act
        var result = issue1.Equals(issue2);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void Issue_Equals_Should_ReturnFalse_When_DifferentId()
    {
        // Arrange
        var issue1 = new Issue { Id = 123, Subject = "Issue 1" };
        var issue2 = new Issue { Id = 456, Subject = "Issue 2" };

        // Act
        var result = issue1.Equals(issue2);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void Issue_Equals_Should_ReturnFalse_When_Null()
    {
        // Arrange
        var issue = new Issue { Id = 123, Subject = "Issue 1" };
        Issue? nullIssue = null;

        // Act
        var result = issue.Equals(nullIssue);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void Issue_Equals_Should_ReturnTrue_When_SameReference()
    {
        // Arrange
        var issue = new Issue { Id = 123, Subject = "Issue 1" };

        // Act
        var result = issue.Equals(issue);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void Issue_Equals_Object_Should_ReturnTrue_When_SameId()
    {
        // Arrange
        var issue1 = new Issue { Id = 123, Subject = "Issue 1" };
        object issue2 = new Issue { Id = 123, Subject = "Different Subject" };

        // Act
        var result = issue1.Equals(issue2);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void Issue_Equals_Object_Should_ReturnFalse_When_NotIssue()
    {
        // Arrange
        var issue = new Issue { Id = 123, Subject = "Issue 1" };
        object notAnIssue = "Not an issue";

        // Act
        var result = issue.Equals(notAnIssue);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void Issue_GetHashCode_Should_ReturnIdHashCode()
    {
        // Arrange
        var issue = new Issue { Id = 123, Subject = "Issue 1" };

        // Act
        var hashCode = issue.GetHashCode();

        // Assert
        hashCode.Should().Be(123.GetHashCode());
    }

    #endregion

    #region Priority Tests

    [Fact]
    public void Priority_Should_HaveCorrectDefaultValues()
    {
        // Arrange
        var priority = new Priority();

        // Act & Assert
        priority.Id.Should().Be(0);
        priority.Name.Should().Be(string.Empty);
    }

    [Fact]
    public void Priority_Should_SetPropertiesCorrectly()
    {
        // Arrange
        var priority = new Priority
        {
            Id = 3,
            Name = "High"
        };

        // Act & Assert
        priority.Id.Should().Be(3);
        priority.Name.Should().Be("High");
    }

    #endregion

    #region IssueStatus Tests

    [Fact]
    public void IssueStatus_Should_HaveCorrectDefaultValues()
    {
        // Arrange
        var status = new IssueStatus();

        // Act & Assert
        status.Id.Should().Be(0);
        status.Name.Should().Be(string.Empty);
        status.IsClosed.Should().BeNull();
    }

    [Fact]
    public void IssueStatus_Should_SetPropertiesCorrectly()
    {
        // Arrange
        var status = new IssueStatus
        {
            Id = 5,
            Name = "Closed",
            IsClosed = true
        };

        // Act & Assert
        status.Id.Should().Be(5);
        status.Name.Should().Be("Closed");
        status.IsClosed.Should().BeTrue();
    }

    #endregion

    #region Preferences Tests

    [Fact]
    public void Preferences_Should_HaveCorrectDefaultValues()
    {
        // Arrange
        var preferences = new Preferences();

        // Act & Assert
        preferences.DefaultFormat.Should().Be("table");
        preferences.PageSize.Should().Be(20);
        preferences.UseColors.Should().BeTrue();
        preferences.DateFormat.Should().Be("yyyy-MM-dd HH:mm:ss");
        preferences.Editor.Should().BeNull();
        preferences.TimeFormat.Should().Be("HH:mm:ss");
    }

    [Fact]
    public void Preferences_Should_SetPropertiesCorrectly()
    {
        // Arrange
        var preferences = new Preferences
        {
            DefaultFormat = "json",
            PageSize = 50,
            UseColors = false,
            DateFormat = "yyyy-MM-dd",
            Editor = "vim",
            TimeFormat = "HH:mm"
        };

        // Act & Assert
        preferences.DefaultFormat.Should().Be("json");
        preferences.PageSize.Should().Be(50);
        preferences.UseColors.Should().BeFalse();
        preferences.DateFormat.Should().Be("yyyy-MM-dd");
        preferences.Editor.Should().Be("vim");
        preferences.TimeFormat.Should().Be("HH:mm");
    }

    #endregion

    #region IssueFilter Tests

    [Fact]
    public void IssueFilter_Should_HaveCorrectDefaultValues()
    {
        // Arrange
        var filter = new IssueFilter();

        // Act & Assert
        filter.AssignedToId.Should().BeNull();
        filter.ProjectId.Should().BeNull();
        filter.StatusId.Should().BeNull();
        filter.Limit.Should().BeNull();
        filter.Offset.Should().BeNull();
    }

    [Fact]
    public void IssueFilter_Should_SetPropertiesCorrectly()
    {
        // Arrange
        var filter = new IssueFilter
        {
            AssignedToId = "me",
            ProjectId = "5",
            StatusId = "open",
            Limit = 25,
            Offset = 10
        };

        // Act & Assert
        filter.AssignedToId.Should().Be("me");
        filter.ProjectId.Should().Be("5");
        filter.StatusId.Should().Be("open");
        filter.Limit.Should().Be(25);
        filter.Offset.Should().Be(10);
    }

    #endregion
}
