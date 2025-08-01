using System.Text;

using FluentAssertions;

using NSubstitute;

using RedmineCLI.ApiClient;
using RedmineCLI.Formatters;
using RedmineCLI.Models;
using RedmineCLI.Utils;

using Spectre.Console;
using Spectre.Console.Testing;

using Xunit;

namespace RedmineCLI.Tests.Formatters;

[Collection("AnsiConsole")]
public class TableFormatterCommentsTests
{
    private readonly TableFormatter _formatter;
    private readonly ITimeHelper _timeHelper;
    private readonly IRedmineApiClient _apiClient;
    private readonly TestConsole _testConsole;

    public TableFormatterCommentsTests()
    {
        _timeHelper = Substitute.For<ITimeHelper>();
        _timeHelper.FormatTime(Arg.Any<DateTime>(), Arg.Any<TimeFormat>())
            .Returns(callInfo => callInfo.ArgAt<DateTime>(0).ToString("yyyy-MM-dd HH:mm"));
        _timeHelper.GetLocalTime(Arg.Any<DateTime>(), Arg.Any<string>())
            .Returns(callInfo => callInfo.ArgAt<DateTime>(0).ToString(callInfo.ArgAt<string>(1)));

        _apiClient = Substitute.For<IRedmineApiClient>();
        _formatter = new TableFormatter(_timeHelper, _apiClient);
        _testConsole = new TestConsole();
        AnsiConsole.Console = _testConsole;
    }

    [Fact]
    public void FormatIssueDetails_Should_ShowOnlyLatestComment_When_ShowAllCommentsIsFalse()
    {
        // Arrange
        var issue = new Issue
        {
            Id = 123,
            Subject = "Test Issue",
            Description = "Test description",
            Status = new IssueStatus { Id = 1, Name = "New" },
            Priority = new Priority { Id = 2, Name = "Normal" },
            AssignedTo = new User { Id = 1, Name = "Test User" },
            Project = new Project { Id = 1, Name = "Test Project" },
            CreatedOn = new DateTime(2024, 1, 1, 10, 0, 0),
            UpdatedOn = new DateTime(2024, 1, 5, 16, 45, 0),
            DoneRatio = 0,
            Journals = new List<Journal>
            {
                new Journal
                {
                    Id = 1,
                    User = new User { Id = 2, Name = "User 1" },
                    Notes = "First comment",
                    CreatedOn = new DateTime(2024, 1, 2, 14, 0, 0),
                    Details = new List<JournalDetail>()
                },
                new Journal
                {
                    Id = 2,
                    User = new User { Id = 3, Name = "User 2" },
                    Notes = null, // Status change only
                    CreatedOn = new DateTime(2024, 1, 3, 10, 0, 0),
                    Details = new List<JournalDetail>
                    {
                        new JournalDetail
                        {
                            Property = "attr",
                            Name = "status_id",
                            OldValue = "1",
                            NewValue = "2"
                        }
                    }
                },
                new Journal
                {
                    Id = 3,
                    User = new User { Id = 4, Name = "User 3" },
                    Notes = "Second comment",
                    CreatedOn = new DateTime(2024, 1, 4, 15, 30, 0),
                    Details = new List<JournalDetail>()
                },
                new Journal
                {
                    Id = 4,
                    User = new User { Id = 5, Name = "User 4" },
                    Notes = "Latest comment",
                    CreatedOn = new DateTime(2024, 1, 5, 16, 45, 0),
                    Details = new List<JournalDetail>()
                }
            }
        };

        // Act
        _formatter.FormatIssueDetails(issue, false, false);
        var output = _testConsole.Output;

        // Assert
        output.Should().Contain("Latest comment");
        output.Should().Contain("Newest comment"); // Check for new label
        output.Should().NotContain("First comment");
        output.Should().NotContain("Second comment");
        output.Should().Contain("Changed status_id from '1' to '2'"); // Status changes should still be shown
        output.Should().Contain("Not showing 2 comments");
        output.Should().Contain("Use --comments to view the full conversation");
    }

    [Fact]
    public void FormatIssueDetails_Should_ShowAllComments_When_ShowAllCommentsIsTrue()
    {
        // Arrange
        var issue = new Issue
        {
            Id = 123,
            Subject = "Test Issue",
            Description = "Test description",
            Status = new IssueStatus { Id = 1, Name = "New" },
            Priority = new Priority { Id = 2, Name = "Normal" },
            AssignedTo = new User { Id = 1, Name = "Test User" },
            Project = new Project { Id = 1, Name = "Test Project" },
            CreatedOn = new DateTime(2024, 1, 1, 10, 0, 0),
            UpdatedOn = new DateTime(2024, 1, 5, 16, 45, 0),
            DoneRatio = 0,
            Journals = new List<Journal>
            {
                new Journal
                {
                    Id = 1,
                    User = new User { Id = 2, Name = "User 1" },
                    Notes = "First comment",
                    CreatedOn = new DateTime(2024, 1, 2, 14, 0, 0),
                    Details = new List<JournalDetail>()
                },
                new Journal
                {
                    Id = 2,
                    User = new User { Id = 3, Name = "User 2" },
                    Notes = "Second comment",
                    CreatedOn = new DateTime(2024, 1, 3, 15, 30, 0),
                    Details = new List<JournalDetail>()
                },
                new Journal
                {
                    Id = 3,
                    User = new User { Id = 4, Name = "User 3" },
                    Notes = "Third comment",
                    CreatedOn = new DateTime(2024, 1, 4, 16, 45, 0),
                    Details = new List<JournalDetail>()
                }
            }
        };

        // Act
        _formatter.FormatIssueDetails(issue, false, true);
        var output = _testConsole.Output;

        // Assert
        output.Should().Contain("First comment");
        output.Should().Contain("Second comment");
        output.Should().Contain("Third comment");
        output.Should().NotContain("Not showing");
        output.Should().NotContain("Use --comments to view the full conversation");
    }

    [Fact]
    public void FormatIssueDetails_Should_NotShowHiddenMessage_When_OnlyOneComment()
    {
        // Arrange
        var issue = new Issue
        {
            Id = 123,
            Subject = "Test Issue",
            Description = "Test description",
            Status = new IssueStatus { Id = 1, Name = "New" },
            Priority = new Priority { Id = 2, Name = "Normal" },
            AssignedTo = new User { Id = 1, Name = "Test User" },
            Project = new Project { Id = 1, Name = "Test Project" },
            CreatedOn = new DateTime(2024, 1, 1, 10, 0, 0),
            UpdatedOn = new DateTime(2024, 1, 2, 14, 0, 0),
            DoneRatio = 0,
            Journals = new List<Journal>
            {
                new Journal
                {
                    Id = 1,
                    User = new User { Id = 2, Name = "User 1" },
                    Notes = "Only comment",
                    CreatedOn = new DateTime(2024, 1, 2, 14, 0, 0),
                    Details = new List<JournalDetail>()
                }
            }
        };

        // Act
        _formatter.FormatIssueDetails(issue, false, false);
        var output = _testConsole.Output;

        // Assert
        output.Should().Contain("Only comment");
        output.Should().NotContain("Not showing");
        output.Should().NotContain("Use --comments to view the full conversation");
    }

    [Fact]
    public void FormatIssueDetails_Should_ShowCorrectCount_When_MultipleCommentsHidden()
    {
        // Arrange
        var issue = new Issue
        {
            Id = 123,
            Subject = "Test Issue",
            Description = "Test description",
            Status = new IssueStatus { Id = 1, Name = "New" },
            Priority = new Priority { Id = 2, Name = "Normal" },
            AssignedTo = new User { Id = 1, Name = "Test User" },
            Project = new Project { Id = 1, Name = "Test Project" },
            CreatedOn = new DateTime(2024, 1, 1, 10, 0, 0),
            UpdatedOn = new DateTime(2024, 1, 7, 10, 0, 0),
            DoneRatio = 0,
            Journals = new List<Journal>
            {
                new Journal { Id = 1, User = new User { Id = 2, Name = "User 1" }, Notes = "Comment 1", CreatedOn = new DateTime(2024, 1, 2, 10, 0, 0), Details = new List<JournalDetail>() },
                new Journal { Id = 2, User = new User { Id = 3, Name = "User 2" }, Notes = "Comment 2", CreatedOn = new DateTime(2024, 1, 3, 10, 0, 0), Details = new List<JournalDetail>() },
                new Journal { Id = 3, User = new User { Id = 4, Name = "User 3" }, Notes = "Comment 3", CreatedOn = new DateTime(2024, 1, 4, 10, 0, 0), Details = new List<JournalDetail>() },
                new Journal { Id = 4, User = new User { Id = 5, Name = "User 4" }, Notes = "Comment 4", CreatedOn = new DateTime(2024, 1, 5, 10, 0, 0), Details = new List<JournalDetail>() },
                new Journal { Id = 5, User = new User { Id = 6, Name = "User 5" }, Notes = "Latest comment", CreatedOn = new DateTime(2024, 1, 6, 10, 0, 0), Details = new List<JournalDetail>() }
            }
        };

        // Act
        _formatter.FormatIssueDetails(issue, false, false);
        var output = _testConsole.Output;

        // Assert
        output.Should().Contain("Latest comment");
        output.Should().NotContain("Comment 1");
        output.Should().NotContain("Comment 2");
        output.Should().NotContain("Comment 3");
        output.Should().NotContain("Comment 4");
        output.Should().Contain("Not showing 4 comments");
    }
}
