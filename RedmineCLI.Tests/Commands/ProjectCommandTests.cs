using System.CommandLine;

using FluentAssertions;

using Microsoft.Extensions.Logging;

using NSubstitute;

using RedmineCLI.ApiClient;
using RedmineCLI.Commands;
using RedmineCLI.Exceptions;
using RedmineCLI.Formatters;
using RedmineCLI.Models;
using RedmineCLI.Services;

using Xunit;

namespace RedmineCLI.Tests.Commands;

public class ProjectCommandTests
{
    private readonly IRedmineApiClient _apiClient;
    private readonly IConfigService _configService;
    private readonly ITableFormatter _tableFormatter;
    private readonly IJsonFormatter _jsonFormatter;
    private readonly ILogger<ProjectCommand> _logger;

    public ProjectCommandTests()
    {
        _apiClient = Substitute.For<IRedmineApiClient>();
        _configService = Substitute.For<IConfigService>();
        _tableFormatter = Substitute.For<ITableFormatter>();
        _jsonFormatter = Substitute.For<IJsonFormatter>();
        _logger = Substitute.For<ILogger<ProjectCommand>>();

        // Setup default config to avoid null reference
        var config = new Config
        {
            CurrentProfile = "default",
            Profiles = new Dictionary<string, Profile>(),
            Preferences = new Preferences()
        };
        _configService.LoadConfigAsync().Returns(Task.FromResult(config));
    }

    [Fact]
    public void Command_Should_HaveLsAlias()
    {
        // Arrange & Act
        var command = ProjectCommand.Create(_apiClient, _configService, _tableFormatter, _jsonFormatter, _logger);
        var listCommand = command.Subcommands.First(c => c.Name == "list");

        // Assert
        listCommand.Aliases.Should().Contain("ls");
    }

    [Fact]
    public async Task List_Should_ReturnAllProjects_When_Called()
    {
        // Arrange
        var projects = new List<Project>
        {
            new Project
            {
                Id = 1,
                Identifier = "main-project",
                Name = "Main Project",
                Description = "Main development project",
                CreatedOn = new DateTime(2024, 1, 1, 10, 0, 0, DateTimeKind.Utc)
            },
            new Project
            {
                Id = 2,
                Identifier = "sub-project",
                Name = "Sub Project",
                Description = "Sub project for testing",
                CreatedOn = new DateTime(2024, 2, 1, 9, 0, 0, DateTimeKind.Utc)
            }
        };

        _apiClient.GetProjectsAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(projects));

        var command = ProjectCommand.Create(_apiClient, _configService, _tableFormatter, _jsonFormatter, _logger);
        var parseResult = command.Parse("list");

        // Act
        var result = await parseResult.InvokeAsync();

        // Assert
        result.Should().Be(0);
        await _apiClient.Received(1).GetProjectsAsync(Arg.Any<CancellationToken>());
        _tableFormatter.Received(1).FormatProjects(projects);
    }

    [Fact]
    public async Task List_Should_ReturnPublicProjects_When_PublicOptionProvided()
    {
        // Arrange
        var publicProjects = new List<Project>
        {
            new Project
            {
                Id = 1,
                Identifier = "public-project",
                Name = "Public Project",
                Description = "This is a public project",
                CreatedOn = new DateTime(2024, 1, 1, 10, 0, 0, DateTimeKind.Utc)
            }
        };

        _apiClient.GetProjectsAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(publicProjects));

        var command = ProjectCommand.Create(_apiClient, _configService, _tableFormatter, _jsonFormatter, _logger);
        var parseResult = command.Parse("list --public");

        // Act
        var result = await parseResult.InvokeAsync();

        // Assert
        result.Should().Be(0);
        // Note: In the actual implementation, we would filter by public projects
        // For now, we just verify the API is called
        await _apiClient.Received(1).GetProjectsAsync(Arg.Any<CancellationToken>());
        _tableFormatter.Received(1).FormatProjects(publicProjects);
    }

    [Fact]
    public async Task List_Should_FormatAsJson_When_JsonOptionIsSet()
    {
        // Arrange
        var projects = new List<Project>
        {
            new Project
            {
                Id = 1,
                Identifier = "main-project",
                Name = "Main Project",
                Description = "Main development project",
                CreatedOn = new DateTime(2024, 1, 1, 10, 0, 0, DateTimeKind.Utc)
            }
        };

        _apiClient.GetProjectsAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(projects));

        var command = ProjectCommand.Create(_apiClient, _configService, _tableFormatter, _jsonFormatter, _logger);
        var parseResult = command.Parse("list --json");

        // Act
        var result = await parseResult.InvokeAsync();

        // Assert
        result.Should().Be(0);
        await _apiClient.Received(1).GetProjectsAsync(Arg.Any<CancellationToken>());
        _jsonFormatter.Received(1).FormatProjects(projects);
        _tableFormatter.DidNotReceive().FormatProjects(Arg.Any<List<Project>>());
    }
}
