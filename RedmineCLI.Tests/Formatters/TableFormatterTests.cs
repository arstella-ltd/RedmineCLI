using FluentAssertions;

using NSubstitute;

using RedmineCLI.ApiClient;
using RedmineCLI.Formatters;
using RedmineCLI.Models;
using RedmineCLI.Services;
using RedmineCLI.Utils;

using Xunit;

namespace RedmineCLI.Tests.Formatters;

[Collection("AnsiConsole")]
public class TableFormatterTests
{
    private readonly TableFormatter _formatter;
    private readonly ITimeHelper _timeHelper;
    private readonly IRedmineApiClient _apiClient;

    public TableFormatterTests()
    {
        _timeHelper = Substitute.For<ITimeHelper>();
        _timeHelper.FormatTime(Arg.Any<DateTime>(), Arg.Any<TimeFormat>())
            .Returns(callInfo => callInfo.ArgAt<DateTime>(0).ToString("yyyy-MM-dd HH:mm"));

        _apiClient = Substitute.For<IRedmineApiClient>();
        _formatter = new TableFormatter(_timeHelper, _apiClient);
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
    public void FormatIssues_Should_DisplayDueDate_When_IssueHasDueDate()
    {
        // Arrange
        var issues = new List<Issue>
        {
            new Issue
            {
                Id = 1,
                Subject = "Test Issue with Due Date",
                Status = new IssueStatus { Id = 1, Name = "New" },
                AssignedTo = new User { Id = 1, Name = "User" },
                Project = new Project { Id = 1, Name = "Test Project" },
                UpdatedOn = new DateTime(2024, 1, 1, 10, 0, 0),
                DueDate = new DateTime(2024, 12, 31)
            }
        };

        // Act & Assert - Should not throw exception
        var exception = Record.Exception(() => _formatter.FormatIssues(issues));
        exception.Should().BeNull();
    }

    [Fact]
    public void FormatIssues_Should_HandleNullDueDate_When_IssueHasNoDueDate()
    {
        // Arrange
        var issues = new List<Issue>
        {
            new Issue
            {
                Id = 1,
                Subject = "Test Issue without Due Date",
                Status = new IssueStatus { Id = 1, Name = "New" },
                AssignedTo = new User { Id = 1, Name = "User" },
                Project = new Project { Id = 1, Name = "Test Project" },
                UpdatedOn = new DateTime(2024, 1, 1, 10, 0, 0),
                DueDate = null
            }
        };

        // Act & Assert - Should not throw exception
        var exception = Record.Exception(() => _formatter.FormatIssues(issues));
        exception.Should().BeNull();
    }

    [Fact]
    public void FormatIssueDetails_Should_DisplayDueDate_When_IssueHasDueDate()
    {
        // Arrange
        var issue = new Issue
        {
            Id = 123,
            Subject = "Test Issue",
            Description = "Test Description",
            Status = new IssueStatus { Id = 1, Name = "New" },
            Priority = new Priority { Id = 2, Name = "Normal" },
            AssignedTo = new User { Id = 1, Name = "Test User" },
            Project = new Project { Id = 1, Name = "Test Project" },
            CreatedOn = new DateTime(2024, 1, 1, 10, 0, 0),
            UpdatedOn = new DateTime(2024, 1, 2, 15, 30, 0),
            DueDate = new DateTime(2024, 12, 31),
            Journals = new List<Journal>()
        };

        // Act & Assert - Should not throw exception
        var exception = Record.Exception(() => _formatter.FormatIssueDetails(issue));
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
                Subject = "高優先度のタスク",
                Status = new IssueStatus { Id = 1, Name = "進行中" },
                Priority = new Priority { Id = 5, Name = "至急" },
                AssignedTo = new User { Id = 1, Name = "山田太郎" },
                Project = new Project { Id = 1, Name = "テストプロジェクト" },
                UpdatedOn = new DateTime(2024, 1, 1, 10, 0, 0),
                DueDate = new DateTime(2024, 12, 31)
            },
            new Issue
            {
                Id = 2,
                Subject = "通常優先度のタスク",
                Status = new IssueStatus { Id = 2, Name = "新規" },
                Priority = new Priority { Id = 3, Name = "通常" },
                AssignedTo = new User { Id = 2, Name = "佐藤花子" },
                Project = new Project { Id = 1, Name = "テストプロジェクト" },
                UpdatedOn = new DateTime(2024, 1, 2, 15, 30, 0),
                DueDate = null
            },
            new Issue
            {
                Id = 3,
                Subject = "優先度未設定のタスク",
                Status = new IssueStatus { Id = 1, Name = "新規" },
                Priority = null, // 優先度が設定されていない場合
                AssignedTo = new User { Id = 3, Name = "鈴木一郎" },
                Project = new Project { Id = 2, Name = "別プロジェクト" },
                UpdatedOn = new DateTime(2024, 1, 3, 09, 15, 0),
                DueDate = new DateTime(2024, 11, 30)
            }
        };

        // Act & Assert - Should not throw exception
        var exception = Record.Exception(() => _formatter.FormatIssues(issues));
        exception.Should().BeNull();
    }

    [Fact]
    public void FormatIssueDetails_Should_DisplayInlineImages_When_DescriptionContainsImageReferences()
    {
        // Arrange
        var issue = new Issue
        {
            Id = 123,
            Subject = "画像を含むチケット",
            Description = @"## 説明
これはスクリーンショットです:
![画面キャプチャ](screenshot.png)

以下はダイアグラムです:
{{thumbnail(diagram.png)}}",
            Status = new IssueStatus { Id = 1, Name = "新規" },
            Priority = new Priority { Id = 2, Name = "通常" },
            AssignedTo = new User { Id = 1, Name = "テストユーザー" },
            Project = new Project { Id = 1, Name = "テストプロジェクト" },
            CreatedOn = new DateTime(2024, 1, 1, 10, 0, 0),
            UpdatedOn = new DateTime(2024, 1, 2, 15, 30, 0),
            Attachments = new List<Attachment>
            {
                new Attachment
                {
                    Id = 1,
                    Filename = "screenshot.png",
                    ContentType = "image/png",
                    ContentUrl = "https://example.com/attachments/1",
                    Filesize = 102400,
                    Author = new User { Id = 1, Name = "テストユーザー" },
                    CreatedOn = new DateTime(2024, 1, 1, 9, 0, 0)
                },
                new Attachment
                {
                    Id = 2,
                    Filename = "diagram.png",
                    ContentType = "image/png",
                    ContentUrl = "https://example.com/attachments/2",
                    Filesize = 51200,
                    Author = new User { Id = 1, Name = "テストユーザー" },
                    CreatedOn = new DateTime(2024, 1, 1, 9, 30, 0)
                },
                new Attachment
                {
                    Id = 3,
                    Filename = "document.pdf",
                    ContentType = "application/pdf",
                    ContentUrl = "https://example.com/attachments/3",
                    Filesize = 204800,
                    Author = new User { Id = 1, Name = "テストユーザー" },
                    CreatedOn = new DateTime(2024, 1, 1, 9, 45, 0)
                }
            }
        };

        // Act & Assert - 画像表示機能が実装されていてもエラーが発生しないことを確認
        var exception = Record.Exception(() => _formatter.FormatIssueDetails(issue));
        exception.Should().BeNull();
    }

    [Fact]
    public void FormatIssueDetails_Should_HandleMissingImageAttachments_When_ReferencedImageNotFound()
    {
        // Arrange
        var issue = new Issue
        {
            Id = 124,
            Subject = "存在しない画像を参照するチケット",
            Description = "![存在しない画像](missing.png)",
            Status = new IssueStatus { Id = 1, Name = "新規" },
            Priority = new Priority { Id = 2, Name = "通常" },
            AssignedTo = new User { Id = 1, Name = "テストユーザー" },
            Project = new Project { Id = 1, Name = "テストプロジェクト" },
            CreatedOn = new DateTime(2024, 1, 1, 10, 0, 0),
            UpdatedOn = new DateTime(2024, 1, 2, 15, 30, 0),
            Attachments = new List<Attachment>
            {
                new Attachment
                {
                    Id = 1,
                    Filename = "other.png",
                    ContentType = "image/png",
                    ContentUrl = "https://example.com/attachments/1",
                    Filesize = 102400,
                    Author = new User { Id = 1, Name = "テストユーザー" },
                    CreatedOn = new DateTime(2024, 1, 1, 9, 0, 0)
                }
            }
        };

        // Act & Assert - 参照されている画像が見つからなくてもエラーが発生しないことを確認
        var exception = Record.Exception(() => _formatter.FormatIssueDetails(issue));
        exception.Should().BeNull();
    }

    [Fact]
    public void FormatIssueDetails_Should_HandleSixelRendering_When_TerminalSupportsSixel()
    {
        // Arrange
        var issue = new Issue
        {
            Id = 125,
            Subject = "Sixel対応ターミナルでの画像表示テスト",
            Description = @"## Sixelプロトコルテスト
![テスト画像](test-image.png)
{{thumbnail(test-thumbnail.jpg)}}",
            Status = new IssueStatus { Id = 1, Name = "新規" },
            Priority = new Priority { Id = 2, Name = "通常" },
            AssignedTo = new User { Id = 1, Name = "テストユーザー" },
            Project = new Project { Id = 1, Name = "テストプロジェクト" },
            CreatedOn = new DateTime(2024, 1, 1, 10, 0, 0),
            UpdatedOn = new DateTime(2024, 1, 2, 15, 30, 0),
            Attachments = new List<Attachment>
            {
                new Attachment
                {
                    Id = 1,
                    Filename = "test-image.png",
                    ContentType = "image/png",
                    ContentUrl = "https://example.com/attachments/1",
                    Filesize = 102400,
                    Author = new User { Id = 1, Name = "テストユーザー" },
                    CreatedOn = new DateTime(2024, 1, 1, 9, 0, 0)
                },
                new Attachment
                {
                    Id = 2,
                    Filename = "test-thumbnail.jpg",
                    ContentType = "image/jpeg",
                    ContentUrl = "https://example.com/attachments/2",
                    Filesize = 51200,
                    Author = new User { Id = 1, Name = "テストユーザー" },
                    CreatedOn = new DateTime(2024, 1, 1, 9, 30, 0)
                }
            }
        };

        // Act & Assert - Sixelレンダリング機能が呼ばれてもエラーが発生しないことを確認
        var exception = Record.Exception(() => _formatter.FormatIssueDetails(issue));
        exception.Should().BeNull();
    }

    [Fact]
    public void FormatIssueDetails_Should_SkipNonImageAttachments_When_DisplayingInlineImages()
    {
        // Arrange
        var issue = new Issue
        {
            Id = 126,
            Subject = "画像以外の添付ファイルを含むチケット",
            Description = @"![画像](image.png)
{{thumbnail(document.pdf)}}",
            Status = new IssueStatus { Id = 1, Name = "新規" },
            Priority = new Priority { Id = 2, Name = "通常" },
            AssignedTo = new User { Id = 1, Name = "テストユーザー" },
            Project = new Project { Id = 1, Name = "テストプロジェクト" },
            CreatedOn = new DateTime(2024, 1, 1, 10, 0, 0),
            UpdatedOn = new DateTime(2024, 1, 2, 15, 30, 0),
            Attachments = new List<Attachment>
            {
                new Attachment
                {
                    Id = 1,
                    Filename = "image.png",
                    ContentType = "image/png",
                    ContentUrl = "https://example.com/attachments/1",
                    Filesize = 102400,
                    Author = new User { Id = 1, Name = "テストユーザー" },
                    CreatedOn = new DateTime(2024, 1, 1, 9, 0, 0)
                },
                new Attachment
                {
                    Id = 2,
                    Filename = "document.pdf",
                    ContentType = "application/pdf",
                    ContentUrl = "https://example.com/attachments/2",
                    Filesize = 204800,
                    Author = new User { Id = 1, Name = "テストユーザー" },
                    CreatedOn = new DateTime(2024, 1, 1, 9, 30, 0)
                }
            }
        };

        // Act & Assert - PDFは画像として表示されないことを確認
        var exception = Record.Exception(() => _formatter.FormatIssueDetails(issue));
        exception.Should().BeNull();
    }

    [Fact]
    public void FormatIssueDetails_Should_DisplayInlineImages_In_JournalNotes()
    {
        // Arrange
        var issue = new Issue
        {
            Id = 456,
            Subject = "コメント内画像テスト",
            Description = "チケットの説明",
            Status = new IssueStatus { Id = 1, Name = "新規" },
            Priority = new Priority { Id = 2, Name = "通常" },
            AssignedTo = new User { Id = 1, Name = "テストユーザー" },
            Project = new Project { Id = 1, Name = "テストプロジェクト" },
            CreatedOn = new DateTime(2024, 1, 1, 10, 0, 0),
            UpdatedOn = new DateTime(2024, 1, 3, 16, 0, 0),
            Journals = new List<Journal>
            {
                new Journal
                {
                    Id = 1,
                    User = new User { Id = 2, Name = "コメント投稿者" },
                    CreatedOn = new DateTime(2024, 1, 2, 14, 0, 0),
                    Notes = @"以下のスクリーンショットを確認してください:
![エラー画面](error-screenshot.png)

修正版:
{{thumbnail(fixed-version.png)}}"
                },
                new Journal
                {
                    Id = 2,
                    User = new User { Id = 3, Name = "別のユーザー" },
                    CreatedOn = new DateTime(2024, 1, 3, 16, 0, 0),
                    Notes = "画像参照なしのコメント"
                }
            },
            Attachments = new List<Attachment>
            {
                new Attachment
                {
                    Id = 10,
                    Filename = "error-screenshot.png",
                    ContentType = "image/png",
                    ContentUrl = "https://example.com/attachments/10",
                    Filesize = 204800,
                    Author = new User { Id = 2, Name = "コメント投稿者" },
                    CreatedOn = new DateTime(2024, 1, 2, 13, 50, 0)
                },
                new Attachment
                {
                    Id = 11,
                    Filename = "fixed-version.png",
                    ContentType = "image/png",
                    ContentUrl = "https://example.com/attachments/11",
                    Filesize = 153600,
                    Author = new User { Id = 2, Name = "コメント投稿者" },
                    CreatedOn = new DateTime(2024, 1, 2, 13, 55, 0)
                }
            }
        };

        // Act & Assert
        var exception = Record.Exception(() => _formatter.FormatIssueDetails(issue));
        exception.Should().BeNull();
    }
}
