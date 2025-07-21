using FluentAssertions;

using RedmineCLI.Formatters;
using RedmineCLI.Models;

using Xunit;

namespace RedmineCLI.Tests.Formatters;

public class JsonFormatterTests
{
    private readonly JsonFormatter _formatter;

    public JsonFormatterTests()
    {
        _formatter = new JsonFormatter();
    }

    [Fact]
    public void FormatIssues_Should_NotThrowException_When_IssuesProvided()
    {
        // Arrange
        var issues = new List<Issue>
        {
            new Issue
            {
                Id = 1,
                Subject = "Test Issue 1",
                Description = "Test Description 1",
                Status = new IssueStatus { Id = 1, Name = "New" },
                Priority = new Priority { Id = 2, Name = "Normal" },
                AssignedTo = new User { Id = 10, Name = "Test User" },
                Project = new Project { Id = 5, Name = "Test Project" },
                CreatedOn = new DateTime(2024, 1, 1, 10, 0, 0),
                UpdatedOn = new DateTime(2024, 1, 2, 15, 30, 0),
                DoneRatio = 25
            },
            new Issue
            {
                Id = 2,
                Subject = "Test Issue 2",
                Description = "Test Description 2",
                Status = new IssueStatus { Id = 2, Name = "In Progress" },
                Priority = new Priority { Id = 3, Name = "High" },
                AssignedTo = new User { Id = 11, Name = "Another User" },
                Project = new Project { Id = 5, Name = "Test Project" },
                CreatedOn = new DateTime(2024, 1, 3, 9, 0, 0),
                UpdatedOn = new DateTime(2024, 1, 4, 11, 0, 0),
                DoneRatio = 50
            }
        };

        // Act & Assert - Should not throw exception
        var exception = Record.Exception(() => _formatter.FormatIssues(issues));
        exception.Should().BeNull();
    }

    [Fact]
    public void FormatIssues_Should_NotThrowException_When_NoIssues()
    {
        // Arrange
        var issues = new List<Issue>();

        // Act & Assert - Should not throw exception
        var exception = Record.Exception(() => _formatter.FormatIssues(issues));
        exception.Should().BeNull();
    }

    [Fact]
    public void FormatIssueDetails_Should_NotThrowException_When_IssueProvided()
    {
        // Arrange
        var issue = new Issue
        {
            Id = 123,
            Subject = "Detailed Test Issue",
            Description = "This is a detailed description with multiple lines.\nLine 2\nLine 3",
            Status = new IssueStatus { Id = 3, Name = "Resolved" },
            Priority = new Priority { Id = 4, Name = "Urgent" },
            AssignedTo = new User { Id = 20, Name = "Assignee Name" },
            Project = new Project { Id = 10, Name = "Detailed Project" },
            CreatedOn = new DateTime(2024, 1, 5, 14, 30, 0),
            UpdatedOn = new DateTime(2024, 1, 10, 16, 45, 0),
            DoneRatio = 75,
            Journals = new List<Journal>
            {
                new Journal
                {
                    Id = 1,
                    User = new User { Id = 40, Name = "Commenter" },
                    Notes = "This is a comment",
                    CreatedOn = new DateTime(2024, 1, 6, 10, 0, 0),
                    Details = new List<JournalDetail>
                    {
                        new JournalDetail
                        {
                            Property = "attr",
                            Name = "status_id",
                            OldValue = "1",
                            NewValue = "3"
                        }
                    }
                }
            }
        };

        // Act & Assert - Should not throw exception
        var exception = Record.Exception(() => _formatter.FormatIssueDetails(issue));
        exception.Should().BeNull();
    }

    [Fact]
    public void FormatIssueDetails_Should_HandleSpecialCharacters_When_IssueContainsJsonSpecialChars()
    {
        // Arrange
        var issue = new Issue
        {
            Id = 456,
            Subject = "Issue with \"quotes\" and \\backslashes\\",
            Description = "Description with special chars: { \"json\": true, \"array\": [1,2,3] }",
            Status = new IssueStatus { Id = 1, Name = "Status with \"quotes\"" },
            Priority = new Priority { Id = 2, Name = "Priority\\with\\backslashes" },
            AssignedTo = new User { Id = 50, Name = "User \"nickname\" Name" },
            Project = new Project { Id = 20, Name = "Project {with} braces" },
            CreatedOn = new DateTime(2024, 1, 1, 10, 0, 0),
            UpdatedOn = new DateTime(2024, 1, 2, 15, 30, 0)
        };

        // Act & Assert - Should not throw exception
        var exception = Record.Exception(() => _formatter.FormatIssueDetails(issue));
        exception.Should().BeNull();
    }

    [Fact]
    public void FormatIssueDetails_Should_HandleJapaneseCharacters_When_IssueContainsJapanese()
    {
        // Arrange
        var issue = new Issue
        {
            Id = 789,
            Subject = "日本語のタイトル",
            Description = "日本語の説明文。てくのかわさき。",
            Status = new IssueStatus { Id = 1, Name = "新規" },
            Priority = new Priority { Id = 2, Name = "通常" },
            AssignedTo = new User { Id = 60, Name = "てくのかわさき" },
            Project = new Project { Id = 30, Name = "日本語プロジェクト" },
            CreatedOn = new DateTime(2024, 1, 1, 10, 0, 0),
            UpdatedOn = new DateTime(2024, 1, 2, 15, 30, 0),
            Journals = new List<Journal>
            {
                new Journal
                {
                    Id = 2,
                    User = new User { Id = 70, Name = "日本語ユーザー" },
                    Notes = "日本語のコメント：てくのかわさき",
                    CreatedOn = new DateTime(2024, 1, 2, 14, 0, 0)
                }
            }
        };

        // Act & Assert - Should not throw exception
        var exception = Record.Exception(() => _formatter.FormatIssueDetails(issue));
        exception.Should().BeNull();
    }

    [Fact]
    public void FormatIssues_Should_HandleNullValues_When_OptionalFieldsAreNull()
    {
        // Arrange
        var issues = new List<Issue>
        {
            new Issue
            {
                Id = 999,
                Subject = "Issue with null fields",
                Description = null,
                Status = new IssueStatus { Id = 1, Name = "New" },
                Priority = new Priority { Id = 2, Name = "Normal" },
                AssignedTo = null,
                Project = new Project { Id = 40, Name = "Test Project" },
                CreatedOn = new DateTime(2024, 1, 1, 10, 0, 0),
                UpdatedOn = new DateTime(2024, 1, 2, 15, 30, 0),
                DoneRatio = null,
                Journals = null
            }
        };

        // Act & Assert - Should not throw exception
        var exception = Record.Exception(() => _formatter.FormatIssues(issues));
        exception.Should().BeNull();
    }
}
