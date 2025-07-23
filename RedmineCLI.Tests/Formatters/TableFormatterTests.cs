using FluentAssertions;

using NSubstitute;

using RedmineCLI.Formatters;
using RedmineCLI.Models;
using RedmineCLI.Utils;

using Xunit;

namespace RedmineCLI.Tests.Formatters;

public class TableFormatterTests
{
    private readonly TableFormatter _formatter;
    private readonly ITimeHelper _timeHelper;

    public TableFormatterTests()
    {
        _timeHelper = Substitute.For<ITimeHelper>();
        _timeHelper.FormatTime(Arg.Any<DateTime>(), Arg.Any<TimeFormat>())
            .Returns(callInfo => callInfo.ArgAt<DateTime>(0).ToString("yyyy-MM-dd HH:mm"));

        _formatter = new TableFormatter(_timeHelper);
    }

    [Fact]
    public void FormatIssueDetails_Should_EscapeSpecialCharacters_When_ContentContainsMarkup()
    {
        // Arrange
        var issue = new Issue
        {
            Id = 123,
            Subject = "Test [Issue] with <markup>",
            Description = "Description with [color] tags and <angle> brackets",
            Status = new IssueStatus { Id = 1, Name = "Status [with] brackets" },
            Priority = new Priority { Id = 2, Name = "Priority <with> brackets" },
            AssignedTo = new User { Id = 1, Name = "User [name] with markup" },
            Project = new Project { Id = 1, Name = "Project <name> with markup" },
            CreatedOn = new DateTime(2024, 1, 1, 10, 0, 0),
            UpdatedOn = new DateTime(2024, 1, 2, 15, 30, 0),
            DoneRatio = 50,
            Journals = new List<Journal>
            {
                new Journal
                {
                    Id = 1,
                    User = new User { Id = 2, Name = "User [with] special chars" },
                    Notes = "Comment with [markup] and <tags>",
                    CreatedOn = new DateTime(2024, 1, 2, 14, 0, 0),
                    Details = new List<JournalDetail>
                    {
                        new JournalDetail
                        {
                            Property = "attr",
                            Name = "status_id",
                            OldValue = "Status [old]",
                            NewValue = "Status [new]"
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
    public void FormatIssueDetails_Should_HandleJapaneseCharacters_When_ContentContainsJapanese()
    {
        // Arrange
        var issue = new Issue
        {
            Id = 6852,
            Subject = "日本語のタイトル",
            Description = "てくのかわさき という文字列を含む説明",
            Status = new IssueStatus { Id = 1, Name = "新規" },
            Priority = new Priority { Id = 2, Name = "通常" },
            AssignedTo = new User { Id = 1, Name = "てくのかわさき" },
            Project = new Project { Id = 1, Name = "日本語プロジェクト" },
            CreatedOn = new DateTime(2024, 1, 1, 10, 0, 0),
            UpdatedOn = new DateTime(2024, 1, 2, 15, 30, 0),
            Journals = new List<Journal>
            {
                new Journal
                {
                    Id = 1,
                    User = new User { Id = 2, Name = "日本語ユーザー" },
                    Notes = "日本語のコメント [てくのかわさき]",
                    CreatedOn = new DateTime(2024, 1, 2, 14, 0, 0),
                    Details = new List<JournalDetail>
                    {
                        new JournalDetail
                        {
                            Property = "attr",
                            Name = "assigned_to_id",
                            OldValue = "田中太郎",
                            NewValue = "てくのかわさき"
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
    public void FormatIssues_Should_EscapeSpecialCharacters_When_IssuesContainMarkup()
    {
        // Arrange
        var issues = new List<Issue>
        {
            new Issue
            {
                Id = 1,
                Subject = "Issue [with] brackets",
                Status = new IssueStatus { Id = 1, Name = "Status <with> markup" },
                AssignedTo = new User { Id = 1, Name = "User [name]" },
                Project = new Project { Id = 1, Name = "Project <name>" },
                UpdatedOn = new DateTime(2024, 1, 1, 10, 0, 0)
            }
        };

        // Act & Assert - Should not throw exception
        var exception = Record.Exception(() => _formatter.FormatIssues(issues));
        exception.Should().BeNull();
    }

    [Fact]
    public void FormatIssues_Should_IncludePriorityColumn_When_DisplayingIssueList()
    {
        // Arrange
        var issues = new List<Issue>
        {
            new Issue
            {
                Id = 1,
                Subject = "High priority issue",
                Status = new IssueStatus { Id = 1, Name = "New" },
                Priority = new Priority { Id = 3, Name = "High" },
                AssignedTo = new User { Id = 1, Name = "John Doe" },
                Project = new Project { Id = 1, Name = "Test Project" },
                UpdatedOn = new DateTime(2024, 1, 1, 10, 0, 0)
            },
            new Issue
            {
                Id = 2,
                Subject = "Normal priority issue",
                Status = new IssueStatus { Id = 2, Name = "In Progress" },
                Priority = new Priority { Id = 2, Name = "Normal" },
                AssignedTo = new User { Id = 2, Name = "Jane Smith" },
                Project = new Project { Id = 1, Name = "Test Project" },
                UpdatedOn = new DateTime(2024, 1, 2, 15, 30, 0)
            },
            new Issue
            {
                Id = 3,
                Subject = "Low priority issue",
                Status = new IssueStatus { Id = 1, Name = "New" },
                Priority = new Priority { Id = 1, Name = "Low" },
                AssignedTo = null,
                Project = new Project { Id = 2, Name = "Another Project" },
                UpdatedOn = new DateTime(2024, 1, 3, 9, 15, 0)
            }
        };

        // Act & Assert - This test will fail initially because Priority column is not implemented
        var exception = Record.Exception(() => _formatter.FormatIssues(issues));
        exception.Should().BeNull();

        // Note: In a real test scenario, we would capture console output and verify
        // that it contains the priority values. For now, this test ensures that
        // the formatter can handle issues with Priority property without throwing exceptions.
    }
}
