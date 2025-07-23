using System.CommandLine;
using System.CommandLine.IO;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using System.Text;
using FluentAssertions;
using NSubstitute;
using RedmineCLI.ApiClient;
using RedmineCLI.Commands;
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
    private readonly IRedmineApiClient _apiClient;
    private readonly ITableFormatter _tableFormatter;
    private readonly IJsonFormatter _jsonFormatter;
    private readonly TestConsole _console;
    private readonly IConsole _systemConsole;
    private readonly AttachmentCommand _attachmentCommand;

    public AttachmentCommandTests()
    {
        _fileSystem = new MockFileSystem();
        _configService = Substitute.For<IConfigService>();
        _apiClient = Substitute.For<IRedmineApiClient>();
        _tableFormatter = Substitute.For<ITableFormatter>();
        _jsonFormatter = Substitute.For<IJsonFormatter>();
        _console = new TestConsole();
        _systemConsole = new TestConsole();

        var profile = new Profile
        {
            Name = "test",
            Url = "https://redmine.example.com",
            ApiKey = "test-key"
        };
        _configService.GetActiveProfileAsync().Returns(Task.FromResult<Profile?>(profile));

        _attachmentCommand = new AttachmentCommand();
    }

    [Fact]
    public async Task Download_Should_SaveFile_When_ValidAttachmentId()
    {
        // Arrange
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
        
        _apiClient.GetAttachmentAsync(123).Returns(attachment);
        _apiClient.DownloadAttachmentAsync(123).Returns(stream);

        var command = new RootCommand();
        var attachmentCommand = _attachmentCommand.CreateCommand(
            _configService, _apiClient, _tableFormatter, _jsonFormatter, _fileSystem);
        command.Add(attachmentCommand);

        // Act
        var result = await command.InvokeAsync("attachment download 123", _systemConsole);

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
        
        _apiClient.GetAttachmentAsync(123).Returns(attachment);
        _apiClient.DownloadAttachmentAsync(123).Returns(stream);
        _fileSystem.AddDirectory("/downloads");

        var command = new RootCommand();
        var attachmentCommand = _attachmentCommand.CreateCommand(
            _configService, _apiClient, _tableFormatter, _jsonFormatter, _fileSystem);
        command.Add(attachmentCommand);

        // Act
        var result = await command.InvokeAsync("attachment download 123 --output /downloads/", _systemConsole);

        // Assert
        result.Should().Be(0);
        _fileSystem.File.Exists("/downloads/test-file.pdf").Should().BeTrue();
    }

    [Fact]
    public async Task Download_Should_ShowProgress_When_Downloading()
    {
        // Arrange
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
        
        _apiClient.GetAttachmentAsync(123).Returns(attachment);
        _apiClient.DownloadAttachmentAsync(123).Returns(stream);

        var command = new RootCommand();
        var attachmentCommand = _attachmentCommand.CreateCommand(
            _configService, _apiClient, _tableFormatter, _jsonFormatter, _fileSystem, _console);
        command.Add(attachmentCommand);

        // Act
        var result = await command.InvokeAsync("attachment download 123", _systemConsole);

        // Assert
        result.Should().Be(0);
        var output = _console.Output;
        output.Should().Contain("Downloading"); // Progress indication should be shown
    }

    [Fact]
    public async Task Download_Should_SanitizeFilename_When_UnsafeCharacters()
    {
        // Arrange
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
        
        _apiClient.GetAttachmentAsync(123).Returns(attachment);
        _apiClient.DownloadAttachmentAsync(123).Returns(stream);

        var command = new RootCommand();
        var attachmentCommand = _attachmentCommand.CreateCommand(
            _configService, _apiClient, _tableFormatter, _jsonFormatter, _fileSystem);
        command.Add(attachmentCommand);

        // Act
        var result = await command.InvokeAsync("attachment download 123", _systemConsole);

        // Assert
        result.Should().Be(0);
        _fileSystem.File.Exists("passwd").Should().BeTrue(); // Sanitized filename
        _fileSystem.File.Exists("../../../etc/passwd").Should().BeFalse(); // Original path should not exist
    }

    [Fact]
    public async Task Download_Should_OverwriteFile_When_ForceOptionProvided()
    {
        // Arrange
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
        
        _apiClient.GetAttachmentAsync(123).Returns(attachment);
        _apiClient.DownloadAttachmentAsync(123).Returns(stream);

        var command = new RootCommand();
        var attachmentCommand = _attachmentCommand.CreateCommand(
            _configService, _apiClient, _tableFormatter, _jsonFormatter, _fileSystem);
        command.Add(attachmentCommand);

        // Act
        var result = await command.InvokeAsync("attachment download 123 --force", _systemConsole);

        // Assert
        result.Should().Be(0);
        var fileContent = _fileSystem.File.ReadAllText("existing-file.txt");
        fileContent.Should().Be("New content");
    }

    [Fact]
    public async Task View_Should_ShowMetadata_When_AttachmentIdProvided()
    {
        // Arrange
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
        
        _apiClient.GetAttachmentAsync(123).Returns(attachment);

        var command = new RootCommand();
        var attachmentCommand = _attachmentCommand.CreateCommand(
            _configService, _apiClient, _tableFormatter, _jsonFormatter, _fileSystem);
        command.Add(attachmentCommand);

        // Act
        var result = await command.InvokeAsync("attachment view 123", _systemConsole);

        // Assert
        result.Should().Be(0);
        await _tableFormatter.Received(1).FormatAttachmentDetailsAsync(attachment);
    }

    [Fact]
    public async Task Download_Should_ReturnError_When_FileExistsAndNoForce()
    {
        // Arrange
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
        
        _apiClient.GetAttachmentAsync(123).Returns(attachment);

        var command = new RootCommand();
        var attachmentCommand = _attachmentCommand.CreateCommand(
            _configService, _apiClient, _tableFormatter, _jsonFormatter, _fileSystem, _console);
        command.Add(attachmentCommand);

        // Act
        var result = await command.InvokeAsync("attachment download 123", _systemConsole);

        // Assert
        result.Should().Be(1);
        var output = _console.Output;
        output.Should().Contain("already exists");
        output.Should().Contain("--force");
    }

    [Fact]
    public async Task Download_Should_HandleNetworkError_When_DownloadFails()
    {
        // Arrange
        _apiClient.GetAttachmentAsync(123).Returns(new Attachment
        {
            Id = 123,
            Filename = "test.txt",
            Filesize = 1024,
            ContentType = "text/plain",
            ContentUrl = "https://redmine.example.com/attachments/download/123/test.txt"
        });
        
        _apiClient.DownloadAttachmentAsync(123).Returns(Task.FromException<Stream>(
            new HttpRequestException("Network error")));

        var command = new RootCommand();
        var attachmentCommand = _attachmentCommand.CreateCommand(
            _configService, _apiClient, _tableFormatter, _jsonFormatter, _fileSystem, _console);
        command.Add(attachmentCommand);

        // Act
        var result = await command.InvokeAsync("attachment download 123", _systemConsole);

        // Assert
        result.Should().Be(1);
        var output = _console.Output;
        output.Should().Contain("Error");
        output.Should().Contain("Network error");
    }

    [Fact]
    public async Task View_Should_FormatAsJson_When_JsonOptionProvided()
    {
        // Arrange
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
        
        _apiClient.GetAttachmentAsync(123).Returns(attachment);

        var command = new RootCommand();
        var attachmentCommand = _attachmentCommand.CreateCommand(
            _configService, _apiClient, _tableFormatter, _jsonFormatter, _fileSystem);
        command.Add(attachmentCommand);

        // Act
        var result = await command.InvokeAsync("attachment view 123 --json", _systemConsole);

        // Assert
        result.Should().Be(0);
        await _jsonFormatter.Received(1).FormatAttachmentDetailsAsync(attachment);
    }
}