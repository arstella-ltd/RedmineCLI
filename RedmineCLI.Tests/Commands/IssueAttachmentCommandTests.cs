using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using FluentAssertions;

using Microsoft.Extensions.Logging;

using NSubstitute;

using RedmineCLI.ApiClient;
using RedmineCLI.Commands;
using RedmineCLI.Formatters;
using RedmineCLI.Models;
using RedmineCLI.Services;
using RedmineCLI.Tests.TestInfrastructure;
using RedmineCLI.Utils;

using Spectre.Console;
using Spectre.Console.Testing;

using Xunit;

namespace RedmineCLI.Tests.Commands;

[Collection("AnsiConsole")]
public class IssueAttachmentCommandTests
{
    private readonly IRedmineApiClient _apiClient;
    private readonly IConfigService _configService;
    private readonly ITableFormatter _tableFormatter;
    private readonly IJsonFormatter _jsonFormatter;
    private readonly ILogger<IssueCommand> _logger;
    private readonly IErrorMessageService _errorMessageService;
    private readonly IssueCommand _issueCommand;
    private readonly AnsiConsoleTestFixture _consoleFixture;

    public IssueAttachmentCommandTests()
    {
        _apiClient = Substitute.For<IRedmineApiClient>();
        _configService = Substitute.For<IConfigService>();
        _tableFormatter = Substitute.For<ITableFormatter>();
        _jsonFormatter = Substitute.For<IJsonFormatter>();
        _logger = Substitute.For<ILogger<IssueCommand>>();
        _errorMessageService = Substitute.For<IErrorMessageService>();

        // デフォルトのプロファイル設定
        var profile = new RedmineCLI.Models.Profile
        {
            Name = "default",
            Url = "https://redmine.example.com",
            ApiKey = "test-api-key"
        };
        _configService.GetActiveProfileAsync().Returns(Task.FromResult<RedmineCLI.Models.Profile?>(profile));

        // デフォルトの設定
        var preferences = new Preferences
        {
            DefaultFormat = "table",
            Time = new TimeSettings { Format = "relative", Timezone = "system" }
        };
        var config = new Config { Preferences = preferences };
        _configService.LoadConfigAsync().Returns(Task.FromResult(config));

        _issueCommand = new IssueCommand(_apiClient, _configService, _tableFormatter, _jsonFormatter, _logger, _errorMessageService);
        _consoleFixture = new AnsiConsoleTestFixture();
    }

    [Fact]
    public async Task ListAttachments_Should_ShowAttachments_When_IssueHasFiles()
    {
        // Arrange
        var issueId = 123;
        var issue = new Issue
        {
            Id = issueId,
            Subject = "Test Issue",
            Attachments = new List<Attachment>
            {
                new() { Id = 1, Filename = "document.pdf", Filesize = 1024000, Author = new User { Name = "John Doe" }, CreatedOn = DateTime.UtcNow.AddDays(-1) },
                new() { Id = 2, Filename = "screenshot.png", Filesize = 512000, Author = new User { Name = "Jane Smith" }, CreatedOn = DateTime.UtcNow.AddHours(-6) }
            }
        };

        _apiClient.GetIssueAsync(issueId, Arg.Any<CancellationToken>()).Returns(Task.FromResult(issue));

        // Act
        var result = await _issueCommand.ListAttachmentsAsync(issueId, false, CancellationToken.None);

        // Assert
        result.Should().Be(0);
        _tableFormatter.Received(1).FormatAttachments(Arg.Is<List<Attachment>>(list =>
            list.Count == 2 &&
            list[0].Filename == "document.pdf" &&
            list[1].Filename == "screenshot.png"));
    }

    [Fact]
    public async Task ListAttachments_Should_ShowEmptyMessage_When_NoAttachments()
    {
        // Arrange
        var issueId = 123;
        var issue = new Issue
        {
            Id = issueId,
            Subject = "Test Issue",
            Attachments = new List<Attachment>()
        };

        _apiClient.GetIssueAsync(issueId, Arg.Any<CancellationToken>()).Returns(Task.FromResult(issue));

        // Act & Assert
        var result = await _consoleFixture.ExecuteWithTestConsoleAsync(async console =>
        {
            var actualResult = await _issueCommand.ListAttachmentsAsync(issueId, false, CancellationToken.None);

            // Assert console output
            console.Output.Should().Contain("No attachments found for issue #123");
            _tableFormatter.DidNotReceive().FormatAttachments(Arg.Any<List<Attachment>>());

            return actualResult;
        });

        // Assert
        result.Should().Be(0);
    }

    [Fact]
    public async Task DownloadAttachments_Should_PromptSelection_When_DefaultMode()
    {
        // Arrange
        var issueId = 123;
        var issue = new Issue
        {
            Id = issueId,
            Subject = "Test Issue",
            Attachments = new List<Attachment>
            {
                new() { Id = 1, Filename = "file1.txt", Filesize = 1024 },
                new() { Id = 2, Filename = "file2.txt", Filesize = 2048 }
            }
        };

        _apiClient.GetIssueAsync(issueId, Arg.Any<CancellationToken>()).Returns(Task.FromResult(issue));

        // Act & Assert
        var result = await _consoleFixture.ExecuteWithTestConsoleAsync(async console =>
        {
            // Simply select with Enter (no selection = empty list)
            console.Input.PushKey(ConsoleKey.Enter);

            var actualResult = await _issueCommand.DownloadAttachmentsAsync(issueId, false, null, CancellationToken.None);

            // Assert console output
            console.Output.Should().Contain("Select attachments to download");
            console.Output.Should().Contain("No attachments selected");

            return actualResult;
        });

        // Assert
        result.Should().Be(0);
    }

    [Fact]
    public async Task DownloadAttachments_Should_DownloadAll_When_AllOptionProvided()
    {
        // Arrange
        var issueId = 123;
        var issue = new Issue
        {
            Id = issueId,
            Subject = "Test Issue",
            Attachments = new List<Attachment>
            {
                new() { Id = 1, Filename = "file1.txt", Filesize = 1024 },
                new() { Id = 2, Filename = "file2.txt", Filesize = 2048 },
                new() { Id = 3, Filename = "file3.txt", Filesize = 3072 }
            }
        };

        _apiClient.GetIssueAsync(issueId, Arg.Any<CancellationToken>()).Returns(Task.FromResult(issue));
        _apiClient.DownloadAttachmentAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(x => new System.IO.MemoryStream(new byte[1024]));

        // Use temp directory for test
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

        // Act & Assert
        var result = await _consoleFixture.ExecuteWithTestConsoleAsync(async console =>
        {
            var actualResult = await _issueCommand.DownloadAttachmentsAsync(issueId, true, tempDir, CancellationToken.None);

            // Assert console output
            console.Output.Should().Contain("Downloaded 3 attachments");

            return actualResult;
        });

        // Assert
        result.Should().Be(0);
        await _apiClient.Received(3).DownloadAttachmentAsync(Arg.Any<int>(), Arg.Any<CancellationToken>());
        await _apiClient.Received(1).DownloadAttachmentAsync(1, Arg.Any<CancellationToken>());
        await _apiClient.Received(1).DownloadAttachmentAsync(2, Arg.Any<CancellationToken>());
        await _apiClient.Received(1).DownloadAttachmentAsync(3, Arg.Any<CancellationToken>());

        // Cleanup
        if (Directory.Exists(tempDir))
            Directory.Delete(tempDir, true);
    }

    [Fact]
    public async Task IssueView_Should_IncludeAttachments_When_AttachmentsExist()
    {
        // Arrange
        var issueId = 123;
        var issue = new Issue
        {
            Id = issueId,
            Subject = "Test Issue",
            Description = "Test description",
            Status = new IssueStatus { Name = "Open" },
            Priority = new Priority { Name = "Normal" },
            Attachments = new List<Attachment>
            {
                new() { Id = 1, Filename = "report.pdf", Filesize = 2048000, Author = new User { Name = "John Doe" }, CreatedOn = DateTime.UtcNow.AddDays(-2) }
            }
        };

        _apiClient.GetIssueAsync(issueId, true, Arg.Any<CancellationToken>()).Returns(Task.FromResult(issue));

        // Act
        var result = await _issueCommand.ViewAsync(issueId, false, false, false, false, CancellationToken.None);

        // Assert
        result.Should().Be(0);
        _tableFormatter.Received(1).FormatIssueDetails(Arg.Is<Issue>(i =>
            i.Id == issueId &&
            i.Attachments != null &&
            i.Attachments.Count == 1 &&
            i.Attachments[0].Filename == "report.pdf"), false);
    }

    [Fact]
    public async Task DownloadAttachments_Should_HandleEmptyAttachments_When_NoFiles()
    {
        // Arrange
        var issueId = 123;
        var issue = new Issue
        {
            Id = issueId,
            Subject = "Test Issue",
            Attachments = new List<Attachment>()
        };

        _apiClient.GetIssueAsync(issueId, Arg.Any<CancellationToken>()).Returns(Task.FromResult(issue));

        // Act & Assert
        var result = await _consoleFixture.ExecuteWithTestConsoleAsync(async console =>
        {
            var actualResult = await _issueCommand.DownloadAttachmentsAsync(issueId, false, null, CancellationToken.None);

            // Assert console output
            console.Output.Should().Contain("No attachments found for issue #123");
            await _apiClient.DidNotReceive().DownloadAttachmentAsync(Arg.Any<int>(), Arg.Any<CancellationToken>());

            return actualResult;
        });

        // Assert
        result.Should().Be(0);
    }

    [Fact]
    public async Task DownloadAttachments_Should_CreateUniqueFilenames_When_DuplicateNames()
    {
        // Arrange
        var issueId = 123;
        var issue = new Issue
        {
            Id = issueId,
            Subject = "Test Issue",
            Attachments = new List<Attachment>
            {
                new() { Id = 1, Filename = "report.pdf", Filesize = 1024 },
                new() { Id = 2, Filename = "report.pdf", Filesize = 2048 } // 同じファイル名
            }
        };

        _apiClient.GetIssueAsync(issueId, Arg.Any<CancellationToken>()).Returns(Task.FromResult(issue));
        _apiClient.DownloadAttachmentAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(x => new System.IO.MemoryStream(new byte[1024]));

        // Use temp directory for test
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

        // Act & Assert
        var result = await _consoleFixture.ExecuteWithTestConsoleAsync(async console =>
        {
            var actualResult = await _issueCommand.DownloadAttachmentsAsync(issueId, true, tempDir, CancellationToken.None);

            // Assert console output
            console.Output.Should().Contain("report.pdf");
            console.Output.Should().Contain("report_1.pdf");

            return actualResult;
        });

        // Assert
        result.Should().Be(0);

        // Cleanup
        if (Directory.Exists(tempDir))
            Directory.Delete(tempDir, true);
    }
}
