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

public class UserCommandTests
{
    private readonly IRedmineApiClient _apiClient;
    private readonly IConfigService _configService;
    private readonly ITableFormatter _tableFormatter;
    private readonly IJsonFormatter _jsonFormatter;
    private readonly ILogger<UserCommand> _logger;

    public UserCommandTests()
    {
        _apiClient = Substitute.For<IRedmineApiClient>();
        _configService = Substitute.For<IConfigService>();
        _tableFormatter = Substitute.For<ITableFormatter>();
        _jsonFormatter = Substitute.For<IJsonFormatter>();
        _logger = Substitute.For<ILogger<UserCommand>>();

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
        var command = UserCommand.Create(_apiClient, _configService, _tableFormatter, _jsonFormatter, _logger);
        var listCommand = command.Subcommands.First(c => c.Name == "list");

        // Assert
        listCommand.Aliases.Should().Contain("ls");
    }

    [Fact]
    public async Task List_Should_ReturnAllUsers_When_Called()
    {
        // Arrange
        var users = new List<User>
        {
            new User
            {
                Id = 1,
                Login = "admin",
                Name = "Administrator",
                Email = "admin@example.com",
                CreatedOn = new DateTime(2024, 1, 1, 10, 0, 0, DateTimeKind.Utc)
            },
            new User
            {
                Id = 2,
                Login = "johndoe",
                Name = "John Doe",
                Email = "john@example.com",
                CreatedOn = new DateTime(2024, 1, 15, 14, 30, 0, DateTimeKind.Utc)
            }
        };

        _apiClient.GetUsersAsync(Arg.Any<int?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(users));

        var command = UserCommand.Create(_apiClient, _configService, _tableFormatter, _jsonFormatter, _logger);
        var parseResult = command.Parse("list");

        // Act
        var result = await parseResult.InvokeAsync();

        // Assert
        result.Should().Be(0);
        await _apiClient.Received(1).GetUsersAsync(30, Arg.Any<CancellationToken>());
        _tableFormatter.Received(1).FormatUsers(users);
    }

    [Fact]
    public async Task List_Should_LimitResults_When_LimitOptionIsSet()
    {
        // Arrange
        var users = new List<User>
        {
            new User
            {
                Id = 1,
                Login = "admin",
                Name = "Administrator",
                Email = "admin@example.com",
                CreatedOn = new DateTime(2024, 1, 1, 10, 0, 0, DateTimeKind.Utc)
            }
        };

        _apiClient.GetUsersAsync(Arg.Any<int?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(users));

        var command = UserCommand.Create(_apiClient, _configService, _tableFormatter, _jsonFormatter, _logger);
        var parseResult = command.Parse("list --limit 10");

        // Act
        var result = await parseResult.InvokeAsync();

        // Assert
        result.Should().Be(0);
        await _apiClient.Received(1).GetUsersAsync(10, Arg.Any<CancellationToken>());
        _tableFormatter.Received(1).FormatUsers(users);
    }

    [Fact]
    public async Task List_Should_FormatAsJson_When_JsonOptionIsSet()
    {
        // Arrange
        var users = new List<User>
        {
            new User
            {
                Id = 1,
                Login = "admin",
                Name = "Administrator",
                Email = "admin@example.com",
                CreatedOn = new DateTime(2024, 1, 1, 10, 0, 0, DateTimeKind.Utc)
            }
        };

        _apiClient.GetUsersAsync(Arg.Any<int?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(users));

        var command = UserCommand.Create(_apiClient, _configService, _tableFormatter, _jsonFormatter, _logger);
        var parseResult = command.Parse("list --json");

        // Act
        var result = await parseResult.InvokeAsync();

        // Assert
        result.Should().Be(0);
        await _apiClient.Received(1).GetUsersAsync(30, Arg.Any<CancellationToken>());
        _jsonFormatter.Received(1).FormatUsers(users);
        _tableFormatter.DidNotReceive().FormatUsers(Arg.Any<List<User>>());
    }

    [Fact]
    public async Task List_Should_HandleApiError_When_UnauthorizedAccess()
    {
        // Arrange
        var apiException = new RedmineApiException(403, "Forbidden", "You do not have permission to view users.");
        _apiClient.GetUsersAsync(Arg.Any<int?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<List<User>>(apiException));

        var command = UserCommand.Create(_apiClient, _configService, _tableFormatter, _jsonFormatter, _logger);
        var parseResult = command.Parse("list");

        // Act
        Environment.ExitCode = 0; // Reset before test
        await parseResult.InvokeAsync();

        // Assert
        Environment.ExitCode.Should().Be(1);
    }

    [Fact]
    public async Task List_Should_AcceptShortHandLimitOption()
    {
        // Arrange
        var users = new List<User>
        {
            new User
            {
                Id = 1,
                Login = "admin",
                Name = "Administrator",
                Email = "admin@example.com",
                CreatedOn = new DateTime(2024, 1, 1, 10, 0, 0, DateTimeKind.Utc)
            }
        };

        _apiClient.GetUsersAsync(Arg.Any<int?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(users));

        var command = UserCommand.Create(_apiClient, _configService, _tableFormatter, _jsonFormatter, _logger);
        var parseResult = command.Parse("list -L 5");

        // Act
        var result = await parseResult.InvokeAsync();

        // Assert
        result.Should().Be(0);
        await _apiClient.Received(1).GetUsersAsync(5, Arg.Any<CancellationToken>());
    }
}
