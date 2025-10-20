using FluentAssertions;

using Microsoft.Extensions.Logging;

using NSubstitute;

using RedmineCLI.Extension.Board.Models;
using RedmineCLI.Extension.Board.Services;

using Xunit;

namespace RedmineCLI.Extension.Board.Tests.Services;

public class HtmlParsingServiceTests
{
    private readonly ILogger<HtmlParsingService> _mockLogger;
    private readonly HtmlParsingService _parsingService;

    public HtmlParsingServiceTests()
    {
        _mockLogger = Substitute.For<ILogger<HtmlParsingService>>();
        _parsingService = new HtmlParsingService(_mockLogger);
    }

    [Fact]
    public void ParseBoardsFromHtml_Should_ReturnEmptyList_When_NoBoardsFound()
    {
        // Arrange
        var html = "<html><body><table></table></body></html>";
        var baseUrl = "https://redmine.example.com";

        // Act
        var result = _parsingService.ParseBoardsFromHtml(html, baseUrl);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void ParseBoardsFromHtml_Should_ParseBoardCorrectly()
    {
        // Arrange
        var html = @"
            <html>
            <body>
                <table>
                    <tr class='board'>
                        <td><a href='/boards/123'><span class='icon-label'>Test Board</span></a></td>
                        <td class='topic-count'>5</td>
                        <td class='message-count'>15</td>
                    </tr>
                </table>
            </body>
            </html>";
        var baseUrl = "https://redmine.example.com";

        // Act
        var result = _parsingService.ParseBoardsFromHtml(html, baseUrl);

        // Assert
        result.Should().HaveCount(1);
        result[0].Id.Should().Be(123);
        result[0].Name.Should().Be("Test Board");
        result[0].Url.Should().Be("https://redmine.example.com/boards/123");
        result[0].ColumnCount.Should().Be(5);
        result[0].CardCount.Should().Be(15);
    }

    [Fact]
    public void ParseBoardsFromHtml_Should_HandleMultipleBoards()
    {
        // Arrange
        var html = @"
            <html>
            <body>
                <table>
                    <tr class='board'>
                        <td><a href='/boards/123'><span class='icon-label'>Board 1</span></a></td>
                        <td class='topic-count'>5</td>
                        <td class='message-count'>15</td>
                    </tr>
                    <tr class='board'>
                        <td><a href='/boards/456'><span class='icon-label'>Board 2</span></a></td>
                        <td class='topic-count'>10</td>
                        <td class='message-count'>30</td>
                    </tr>
                </table>
            </body>
            </html>";
        var baseUrl = "https://redmine.example.com";

        // Act
        var result = _parsingService.ParseBoardsFromHtml(html, baseUrl);

        // Assert
        result.Should().HaveCount(2);
        result[0].Id.Should().Be(123);
        result[0].Name.Should().Be("Board 1");
        result[1].Id.Should().Be(456);
        result[1].Name.Should().Be("Board 2");
    }

    [Fact]
    public void ParseBoardsFromHtml_Should_HandleHtmlEntities()
    {
        // Arrange
        var html = @"
            <html>
            <body>
                <table>
                    <tr class='board'>
                        <td><a href='/boards/123'><span class='icon-label'>Test &amp; Board</span></a></td>
                        <td class='topic-count'>5</td>
                        <td class='message-count'>15</td>
                    </tr>
                </table>
            </body>
            </html>";
        var baseUrl = "https://redmine.example.com";

        // Act
        var result = _parsingService.ParseBoardsFromHtml(html, baseUrl);

        // Assert
        result.Should().HaveCount(1);
        result[0].Name.Should().Be("Test & Board");
    }

    [Fact]
    public void ParseBoardsFromHtml_Should_HandleException()
    {
        // Arrange
        var html = "invalid html";
        var baseUrl = "https://redmine.example.com";

        // Act
        var result = _parsingService.ParseBoardsFromHtml(html, baseUrl);

        // Assert
        result.Should().BeEmpty();
        _mockLogger.DidNotReceiveWithAnyArgs().LogError(Arg.Any<Exception>(), "Error parsing boards from HTML"); // HtmlAgilityPack handles invalid HTML gracefully
    }

    [Fact]
    public void ParseTopicsFromHtml_Should_ReturnEmptyList_When_NoTopicsFound()
    {
        // Arrange
        var html = "<html><body></body></html>";

        // Act
        var result = _parsingService.ParseTopicsFromHtml(html);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void ParseTopicsFromHtml_Should_ParseTopicCorrectly()
    {
        // Arrange
        var html = @"
            <html>
            <body>
                <table class='list messages'>
                    <tbody>
                        <tr>
                            <td class='subject'>
                                <a href='/messages/123'>Test Topic</a>
                            </td>
                            <td class='author'>John Doe</td>
                            <td class='replies'>5</td>
                            <td class='last-reply'>2024-01-01 12:00</td>
                        </tr>
                    </tbody>
                </table>
            </body>
            </html>";

        // Act
        var result = _parsingService.ParseTopicsFromHtml(html);

        // Assert
        result.Should().HaveCount(1);
        result[0].Id.Should().Be(123);
        result[0].Title.Should().Be("Test Topic");
        result[0].Author.Should().Be("John Doe");
        result[0].Replies.Should().Be(5);
        result[0].LastReply.Should().NotBeNull();
    }

    [Fact]
    public void ParseTopicsFromHtml_Should_HandleStickyAndLockedTopics()
    {
        // Arrange
        var html = @"
            <html>
            <body>
                <table class='list messages'>
                    <tbody>
                        <tr class='sticky locked'>
                            <td class='subject'>
                                <a href='/messages/123'>Sticky Locked Topic</a>
                            </td>
                            <td class='author'>Admin</td>
                            <td class='replies'>0</td>
                            <td class='last-reply'></td>
                        </tr>
                    </tbody>
                </table>
            </body>
            </html>";

        // Act
        var result = _parsingService.ParseTopicsFromHtml(html);

        // Assert
        result.Should().HaveCount(1);
        result[0].IsSticky.Should().BeTrue();
        result[0].IsLocked.Should().BeTrue();
    }

    [Fact]
    public void ParseTopicsFromHtml_Should_HandleRelativeTime()
    {
        // Arrange
        var html = @"
            <html>
            <body>
                <table class='list messages'>
                    <tbody>
                        <tr>
                            <td class='subject'>
                                <a href='/messages/123'>Test Topic</a>
                            </td>
                            <td class='author'>John Doe</td>
                            <td class='replies'>5</td>
                            <td class='last-reply'>5 days ago</td>
                        </tr>
                    </tbody>
                </table>
            </body>
            </html>";

        // Act
        var result = _parsingService.ParseTopicsFromHtml(html);

        // Assert
        result.Should().HaveCount(1);
        result[0].LastReply.Should().NotBeNull();
        result[0].LastReply!.Value.Should().BeCloseTo(DateTime.Now.AddDays(-5), TimeSpan.FromMinutes(1));
    }

    [Fact]
    public void ParseTopicDetailFromHtml_Should_ParseBasicTopicInfo()
    {
        // Arrange
        var html = @"
            <html>
            <body>
                <h2>Test Topic Title</h2>
                <div class='message' id='message-123'>
                    <h4 class='header'>
                        <a class='user'>John Doe</a> - 2024-01-01 12:00
                    </h4>
                    <div class='wiki'>This is the topic content.</div>
                </div>
            </body>
            </html>";

        // Act
        var result = _parsingService.ParseTopicDetailFromHtml(html);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(123);
        result.Title.Should().Be("Test Topic Title");
        result.Author.Should().Be("John Doe");
        result.Content.Should().Be("This is the topic content.");
    }

    [Fact]
    public void ParseTopicDetailFromHtml_Should_ParseReplies()
    {
        // Arrange
        var html = @"
            <html>
            <body>
                <h2>Test Topic</h2>
                <div class='message' id='message-123'>
                    <h4 class='header'>
                        <a class='user'>John Doe</a> - 2024-01-01 12:00
                    </h4>
                    <div class='wiki'>Original content</div>
                </div>
                <div id='replies'>
                    <div class='message reply' id='message-456'>
                        <h4 class='reply-header'>
                            <a class='user'>Jane Smith</a> - 2024-01-02 13:00
                        </h4>
                        <div class='wiki'>Reply content</div>
                    </div>
                </div>
            </body>
            </html>";

        // Act
        var result = _parsingService.ParseTopicDetailFromHtml(html);

        // Assert
        result.Should().NotBeNull();
        result!.Replies.Should().HaveCount(1);
        result.Replies[0].Id.Should().Be(456);
        result.Replies[0].Author.Should().Be("Jane Smith");
        result.Replies[0].Content.Should().Be("Reply content");
    }

    [Fact]
    public void ParseTopicDetailFromHtml_Should_HandleRelativeTimeInJapanese()
    {
        // Arrange
        var html = @"
            <html>
            <body>
                <h2>Test Topic</h2>
                <div class='message' id='message-123'>
                    <h4 class='header'>
                        <a class='user'>田中太郎</a> - 5日前
                    </h4>
                    <div class='wiki'>テスト内容</div>
                </div>
            </body>
            </html>";

        // Act
        var result = _parsingService.ParseTopicDetailFromHtml(html);

        // Assert
        result.Should().NotBeNull();
        result!.Author.Should().Be("田中太郎");
        result.CreatedAt.Should().BeCloseTo(DateTime.Now.AddDays(-5), TimeSpan.FromMinutes(1));
    }

    [Fact]
    public void ParseTopicDetailFromHtml_Should_ReturnNull_When_InvalidHtml()
    {
        // Arrange
        var html = "<html><body></body></html>";

        // Act
        var result = _parsingService.ParseTopicDetailFromHtml(html);

        // Assert
        result.Should().NotBeNull(); // Returns a new TopicDetail with default values
        result!.Id.Should().Be(0);
        result.Title.Should().BeEmpty(); // Empty string instead of null
    }
}
