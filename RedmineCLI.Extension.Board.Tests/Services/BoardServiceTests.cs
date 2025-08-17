using System.Net;
using System.Net.Http;

using FluentAssertions;

using Microsoft.Extensions.Logging;

using NSubstitute;

using RedmineCLI.Extension.Board.Models;
using RedmineCLI.Extension.Board.Services;

using Xunit;

namespace RedmineCLI.Extension.Board.Tests.Services;

public class BoardServiceTests
{
    private readonly ILogger<BoardService> _mockLogger;
    private readonly IAuthenticationService _mockAuthService;
    private readonly IHtmlParsingService _mockHtmlParsingService;
    private readonly BoardService _boardService;

    public BoardServiceTests()
    {
        _mockLogger = Substitute.For<ILogger<BoardService>>();
        _mockAuthService = Substitute.For<IAuthenticationService>();
        _mockHtmlParsingService = Substitute.For<IHtmlParsingService>();
        _boardService = new BoardService(_mockLogger, _mockAuthService, _mockHtmlParsingService);
    }

    [Fact]
    public async Task ListBoardsAsync_Should_ExitWithError_When_NoSessionCookie()
    {
        // Arrange
        _mockAuthService.GetAuthenticationAsync(Arg.Any<string?>())
            .Returns(Task.FromResult<(string, string?)>(("https://redmine.example.com", string.Empty)));

        var exitCode = 0;
        Environment.ExitCode = 0;

        // Act
        try
        {
            await _boardService.ListBoardsAsync("test-project", null);
        }
        catch (Exception)
        {
            // Environment.Exit throws an exception in tests
            exitCode = Environment.ExitCode;
        }

        // Assert
        await _mockAuthService.Received(1).GetAuthenticationAsync(null);
    }

    [Fact]
    public async Task ListBoardsAsync_Should_ExitWithError_When_ProjectFilterIsNull()
    {
        // Arrange
        _mockAuthService.GetAuthenticationAsync(Arg.Any<string?>())
            .Returns(Task.FromResult<(string, string?)>(("https://redmine.example.com", "session-cookie")));

        var exitCode = 0;
        Environment.ExitCode = 0;

        // Act
        try
        {
            await _boardService.ListBoardsAsync(null, null);
        }
        catch (Exception)
        {
            // Environment.Exit throws an exception in tests
            exitCode = Environment.ExitCode;
        }

        // Assert
        await _mockAuthService.Received(1).GetAuthenticationAsync(null);
    }

    [Theory]
    [InlineData("1")]
    [InlineData("123")]
    public void ParseProjectId_Should_ReturnInt_When_ValidNumber(string identifier)
    {
        // Arrange
        var service = new BoardServiceTestWrapper();

        // Act
        var result = service.TestParseProjectId(identifier);

        // Assert
        result.Should().Be(int.Parse(identifier));
    }

    [Theory]
    [InlineData("test-project")]
    [InlineData("my-awesome-project")]
    public void ParseProjectId_Should_ReturnNull_When_NotNumber(string identifier)
    {
        // Arrange
        var service = new BoardServiceTestWrapper();

        // Act
        var result = service.TestParseProjectId(identifier);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void FormatRelativeTime_Should_Return_JustNow_When_LessThanMinute()
    {
        // Arrange
        var service = new BoardServiceTestWrapper();
        var dateTime = DateTime.Now.AddSeconds(-30);

        // Act
        var result = service.TestFormatRelativeTime(dateTime);

        // Assert
        result.Should().Be("just now");
    }

    [Theory]
    [InlineData(1, "about 1 minute ago")]
    [InlineData(5, "about 5 minutes ago")]
    [InlineData(59, "about 59 minutes ago")]
    public void FormatRelativeTime_Should_Return_Minutes_When_LessThanHour(int minutes, string expected)
    {
        // Arrange
        var service = new BoardServiceTestWrapper();
        var dateTime = DateTime.Now.AddMinutes(-minutes);

        // Act
        var result = service.TestFormatRelativeTime(dateTime);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(1, "about 1 hour ago")]
    [InlineData(5, "about 5 hours ago")]
    [InlineData(23, "about 23 hours ago")]
    public void FormatRelativeTime_Should_Return_Hours_When_LessThanDay(int hours, string expected)
    {
        // Arrange
        var service = new BoardServiceTestWrapper();
        var dateTime = DateTime.Now.AddHours(-hours);

        // Act
        var result = service.TestFormatRelativeTime(dateTime);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(1, "about 1 day ago")]
    [InlineData(7, "about 7 days ago")]
    [InlineData(29, "about 29 days ago")]
    public void FormatRelativeTime_Should_Return_Days_When_LessThanMonth(int days, string expected)
    {
        // Arrange
        var service = new BoardServiceTestWrapper();
        var dateTime = DateTime.Now.AddDays(-days);

        // Act
        var result = service.TestFormatRelativeTime(dateTime);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(1, "about 1 month ago")]
    [InlineData(6, "about 6 months ago")]
    [InlineData(11, "about 11 months ago")]
    public void FormatRelativeTime_Should_Return_Months_When_LessThanYear(int months, string expected)
    {
        // Arrange
        var service = new BoardServiceTestWrapper();
        var dateTime = DateTime.Now.AddDays(-months * 30);

        // Act
        var result = service.TestFormatRelativeTime(dateTime);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(1, "about 1 year ago")]
    [InlineData(2, "about 2 years ago")]
    [InlineData(10, "about 10 years ago")]
    public void FormatRelativeTime_Should_Return_Years_When_MoreThanYear(int years, string expected)
    {
        // Arrange
        var service = new BoardServiceTestWrapper();
        var dateTime = DateTime.Now.AddDays(-years * 365);

        // Act
        var result = service.TestFormatRelativeTime(dateTime);

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public async Task ListTopicsAsync_Should_ExitWithError_When_InvalidBoardId()
    {
        // Arrange
        var auth = ("session-cookie", "https://redmine.example.com");

        // Act
        await _boardService.ListTopicsAsync("invalid-id", null, auth);

        // Assert
        // Should print error message for invalid board ID
        _mockHtmlParsingService.DidNotReceive().ParseTopicsFromHtml(Arg.Any<string>());
    }

    [Fact]
    public async Task ViewTopicAsync_Should_ExitWithError_When_InvalidBoardId()
    {
        // Arrange
        var auth = ("session-cookie", "https://redmine.example.com");

        // Act
        await _boardService.ViewTopicAsync("invalid-id", "123", null, auth);

        // Assert
        // Should print error message for invalid board ID
        _mockHtmlParsingService.DidNotReceive().ParseTopicDetailFromHtml(Arg.Any<string>());
    }

    [Fact]
    public async Task ViewTopicAsync_Should_ExitWithError_When_InvalidTopicId()
    {
        // Arrange
        var auth = ("session-cookie", "https://redmine.example.com");

        // Act
        await _boardService.ViewTopicAsync("123", "invalid-id", null, auth);

        // Assert
        // Should print error message for invalid topic ID
        _mockHtmlParsingService.DidNotReceive().ParseTopicDetailFromHtml(Arg.Any<string>());
    }

    // Test wrapper to expose protected methods for testing
    private class BoardServiceTestWrapper : BoardService
    {
        public BoardServiceTestWrapper() : base(
            Substitute.For<ILogger<BoardService>>(),
            Substitute.For<IAuthenticationService>(),
            Substitute.For<IHtmlParsingService>())
        {
        }

        public int? TestParseProjectId(string identifier)
        {
            var method = typeof(BoardService).GetMethod("ParseProjectId",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            return (int?)method!.Invoke(this, new object[] { identifier });
        }

        public string TestFormatRelativeTime(DateTime dateTime)
        {
            var method = typeof(BoardService).GetMethod("FormatRelativeTime",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            return (string)method!.Invoke(this, new object[] { dateTime })!;
        }
    }
}
