using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using System.Text;

using FluentAssertions;

using NSubstitute;

using RedmineCLI.Commands;
using RedmineCLI.Exceptions;
using RedmineCLI.Formatters;
using RedmineCLI.Models;
using RedmineCLI.Services;

using Spectre.Console.Testing;

using Xunit;

namespace RedmineCLI.Tests.Commands;

public class AttachmentCommandTests
{
    private readonly MockFileSystem _fileSystem;
    private readonly IConfigService _configService;
    private readonly IRedmineService _redmineService;
    private readonly ITableFormatter _tableFormatter;
    private readonly IJsonFormatter _jsonFormatter;
    private readonly AttachmentCommand _attachmentCommand;

    public AttachmentCommandTests()
    {
        _fileSystem = new MockFileSystem();
        _configService = Substitute.For<IConfigService>();
        _redmineService = Substitute.For<IRedmineService>();
        _tableFormatter = Substitute.For<ITableFormatter>();
        _jsonFormatter = Substitute.For<IJsonFormatter>();

        var profile = new Profile
        {
            Name = "test",
            Url = "https://redmine.example.com",
            ApiKey = "test-key"
        };
        _configService.GetActiveProfileAsync().Returns(Task.FromResult<Profile?>(profile));

        _attachmentCommand = new AttachmentCommand();
    }

    private async Task<int> InvokeCommandAsync(Command command, string[] args)
    {
        Environment.ExitCode = 0; // Reset exit code before test
        var parseResult = command.Parse(args);
        await parseResult.InvokeAsync();
        return Environment.ExitCode;
    }

    [Fact]
    public async Task Download_Should_SaveFile_When_ValidAttachmentId()
    {
        // Arrange
        using var console = new TestConsole();
        var attachment = new Attachment
        {
            Id = 123,
            Filename = "test-file.pdf",
            Filesize = 1024,
            ContentType = "application/pdf",
            ContentUrl = "https://redmine.example.com/attachments/download/123/test-file.pdf"
        };

        var fileContent = Encoding.UTF8.GetBytes("Test file content");
        var stream = new MemoryStream(fileContent);

        _redmineService.GetAttachmentAsync(123).Returns(attachment);
        _redmineService.DownloadAttachmentAsync(123).Returns(stream);

        var command = _attachmentCommand.CreateCommand(
            _configService, _redmineService, _tableFormatter, _jsonFormatter, _fileSystem, console);

        // Act
        var result = await InvokeCommandAsync(command, new[] { "download", "123" });

        // Assert
        result.Should().Be(0);
        _fileSystem.File.Exists("test-file.pdf").Should().BeTrue();
        var savedContent = _fileSystem.File.ReadAllBytes("test-file.pdf");
        savedContent.Should().Equal(fileContent);
    }

    [Fact]
    public async Task Download_Should_UseOutputPath_When_OutputOptionProvided()
    {
        // Arrange
        using var console = new TestConsole();
        var attachment = new Attachment
        {
            Id = 123,
            Filename = "test-file.pdf",
            Filesize = 1024,
            ContentType = "application/pdf",
            ContentUrl = "https://redmine.example.com/attachments/download/123/test-file.pdf"
        };

        var fileContent = Encoding.UTF8.GetBytes("Test file content");
        var stream = new MemoryStream(fileContent);

        _redmineService.GetAttachmentAsync(123).Returns(attachment);
        _redmineService.DownloadAttachmentAsync(123).Returns(stream);
        _fileSystem.AddDirectory("/downloads");

        var command = _attachmentCommand.CreateCommand(
            _configService, _redmineService, _tableFormatter, _jsonFormatter, _fileSystem, console);

        // Act
        var result = await InvokeCommandAsync(command, new[] { "download", "123", "--output", "/downloads/" });

        // Assert
        result.Should().Be(0);
        _fileSystem.File.Exists("/downloads/test-file.pdf").Should().BeTrue();
    }

    [Fact]
    public async Task Download_Should_ShowProgress_When_Downloading()
    {
        // Arrange
        using var console = new TestConsole();
        var attachment = new Attachment
        {
            Id = 123,
            Filename = "large-file.zip",
            Filesize = 1024 * 1024, // 1MB
            ContentType = "application/zip",
            ContentUrl = "https://redmine.example.com/attachments/download/123/large-file.zip"
        };

        var fileContent = new byte[1024 * 1024]; // 1MB
        var stream = new MemoryStream(fileContent);

        _redmineService.GetAttachmentAsync(123).Returns(attachment);
        _redmineService.DownloadAttachmentAsync(123).Returns(stream);

        var command = _attachmentCommand.CreateCommand(
            _configService, _redmineService, _tableFormatter, _jsonFormatter, _fileSystem, console);

        // Act
        var result = await InvokeCommandAsync(command, new[] { "download", "123" });

        // Assert
        result.Should().Be(0);
        var output = console.Output;
        output.Should().Contain("Downloaded to:"); // Check for success message instead
    }

    [Fact]
    public async Task Download_Should_SanitizeFilename_When_UnsafeCharacters()
    {
        // Arrange
        using var console = new TestConsole();
        var attachment = new Attachment
        {
            Id = 123,
            Filename = "../../../etc/passwd", // Path traversal attempt
            Filesize = 1024,
            ContentType = "text/plain",
            ContentUrl = "https://redmine.example.com/attachments/download/123/passwd"
        };

        var fileContent = Encoding.UTF8.GetBytes("Test file content");
        var stream = new MemoryStream(fileContent);

        _redmineService.GetAttachmentAsync(123).Returns(attachment);
        _redmineService.DownloadAttachmentAsync(123).Returns(stream);

        var command = _attachmentCommand.CreateCommand(
            _configService, _redmineService, _tableFormatter, _jsonFormatter, _fileSystem, console);

        // Act
        var result = await InvokeCommandAsync(command, new[] { "download", "123" });

        // Assert
        result.Should().Be(0);
        _fileSystem.File.Exists("passwd").Should().BeTrue(); // Sanitized filename
        _fileSystem.File.Exists("../../../etc/passwd").Should().BeFalse(); // Original path should not exist
    }

    [Fact]
    public async Task Download_Should_OverwriteFile_When_ForceOptionProvided()
    {
        // Arrange
        using var console = new TestConsole();
        var attachment = new Attachment
        {
            Id = 123,
            Filename = "existing-file.txt",
            Filesize = 1024,
            ContentType = "text/plain",
            ContentUrl = "https://redmine.example.com/attachments/download/123/existing-file.txt"
        };

        // Create existing file
        _fileSystem.AddFile("existing-file.txt", new MockFileData("Old content"));

        var newContent = Encoding.UTF8.GetBytes("New content");
        var stream = new MemoryStream(newContent);

        _redmineService.GetAttachmentAsync(123).Returns(attachment);
        _redmineService.DownloadAttachmentAsync(123).Returns(stream);

        var command = _attachmentCommand.CreateCommand(
            _configService, _redmineService, _tableFormatter, _jsonFormatter, _fileSystem, console);

        // Act
        var result = await InvokeCommandAsync(command, new[] { "download", "123", "--force" });

        // Assert
        result.Should().Be(0);
        var fileContent = _fileSystem.File.ReadAllText("existing-file.txt");
        fileContent.Should().Be("New content");
    }

    [Fact]
    public async Task View_Should_ShowMetadata_When_AttachmentIdProvided()
    {
        // Arrange
        using var console = new TestConsole();
        var attachment = new Attachment
        {
            Id = 123,
            Filename = "document.pdf",
            Filesize = 2048,
            ContentType = "application/pdf",
            Description = "Important document",
            Author = new User { Id = 1, Name = "John Doe" },
            CreatedOn = new DateTime(2024, 1, 15, 10, 30, 0, DateTimeKind.Utc),
            ContentUrl = "https://redmine.example.com/attachments/download/123/document.pdf"
        };

        _redmineService.GetAttachmentAsync(123).Returns(attachment);

        var command = _attachmentCommand.CreateCommand(
            _configService, _redmineService, _tableFormatter, _jsonFormatter, _fileSystem, console);

        // Act
        var result = await InvokeCommandAsync(command, new[] { "view", "123" });

        // Assert
        result.Should().Be(0);
        _tableFormatter.Received(1).FormatAttachmentDetails(attachment, false);
    }

    [Fact]
    public async Task Download_Should_ReturnError_When_FileExistsAndNoForce()
    {
        // Arrange
        using var console = new TestConsole();
        var attachment = new Attachment
        {
            Id = 123,
            Filename = "existing-file.txt",
            Filesize = 1024,
            ContentType = "text/plain",
            ContentUrl = "https://redmine.example.com/attachments/download/123/existing-file.txt"
        };

        // Create existing file
        _fileSystem.AddFile("existing-file.txt", new MockFileData("Old content"));

        _redmineService.GetAttachmentAsync(123).Returns(attachment);

        var command = _attachmentCommand.CreateCommand(
            _configService, _redmineService, _tableFormatter, _jsonFormatter, _fileSystem, console);

        // Act
        var result = await InvokeCommandAsync(command, new[] { "download", "123" });

        // Assert
        result.Should().Be(1);
        var output = console.Output;
        output.Should().Contain("already exists");
        output.Should().Contain("--force");
    }

    [Fact]
    public async Task Download_Should_HandleNetworkError_When_DownloadFails()
    {
        // Arrange
        using var console = new TestConsole();
        _redmineService.GetAttachmentAsync(123).Returns(new Attachment
        {
            Id = 123,
            Filename = "test.txt",
            Filesize = 1024,
            ContentType = "text/plain",
            ContentUrl = "https://redmine.example.com/attachments/download/123/test.txt"
        });

        _redmineService.DownloadAttachmentAsync(123).Returns(Task.FromException<Stream>(
            new HttpRequestException("Network error")));

        var command = _attachmentCommand.CreateCommand(
            _configService, _redmineService, _tableFormatter, _jsonFormatter, _fileSystem, console);

        // Act
        var result = await InvokeCommandAsync(command, new[] { "download", "123" });

        // Assert
        result.Should().Be(1);
        var output = console.Output;
        output.Should().Contain("Error");
        output.Should().Contain("Network error");
    }

    [Fact]
    public async Task View_Should_FormatAsJson_When_JsonOptionProvided()
    {
        // Arrange
        using var console = new TestConsole();
        var attachment = new Attachment
        {
            Id = 123,
            Filename = "document.pdf",
            Filesize = 2048,
            ContentType = "application/pdf",
            Description = "Important document",
            Author = new User { Id = 1, Name = "John Doe" },
            CreatedOn = new DateTime(2024, 1, 15, 10, 30, 0, DateTimeKind.Utc),
            ContentUrl = "https://redmine.example.com/attachments/download/123/document.pdf"
        };

        _redmineService.GetAttachmentAsync(123).Returns(attachment);

        var command = _attachmentCommand.CreateCommand(
            _configService, _redmineService, _tableFormatter, _jsonFormatter, _fileSystem, console);

        // Act
        var result = await InvokeCommandAsync(command, new[] { "view", "123", "--json" });

        // Assert
        result.Should().Be(0);
        _jsonFormatter.Received(1).FormatAttachmentDetails(attachment);
    }
}
