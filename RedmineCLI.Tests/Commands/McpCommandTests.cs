using System.CommandLine;

using FluentAssertions;

using Microsoft.Extensions.Logging;

using NSubstitute;

using RedmineCLI.Commands;
using RedmineCLI.Services;
using RedmineCLI.Services.Mcp;

using Xunit;

namespace RedmineCLI.Tests.Commands;

public class McpCommandTests
{
    private readonly IRedmineService _mockRedmineService;
    private readonly ILogger<McpCommand> _mockLogger;
    private readonly ILogger<McpServer> _mockMcpServerLogger;

    public McpCommandTests()
    {
        _mockRedmineService = Substitute.For<IRedmineService>();
        _mockLogger = Substitute.For<ILogger<McpCommand>>();
        _mockMcpServerLogger = Substitute.For<ILogger<McpServer>>();
    }

    [Fact]
    public void Create_Should_ReturnCommand()
    {
        // Act
        var command = McpCommand.Create(_mockRedmineService, _mockLogger, _mockMcpServerLogger);

        // Assert
        command.Should().NotBeNull();
        command.Name.Should().Be("mcp");
        command.Description.Should().Be("Start MCP (Model Context Protocol) server");
    }

    [Fact]
    public void Create_Should_HaveDebugOption()
    {
        // Act
        var command = McpCommand.Create(_mockRedmineService, _mockLogger, _mockMcpServerLogger);

        // Assert
        var debugOption = command.Options.FirstOrDefault(o => o.Name == "--debug");
        debugOption.Should().NotBeNull();
        debugOption!.ValueType.Should().Be(typeof(bool));
    }

    [Fact]
    public void Constructor_Should_InitializeFields()
    {
        // Act
        var mcpCommand = new McpCommand(_mockRedmineService, _mockLogger, _mockMcpServerLogger);

        // Assert
        mcpCommand.Should().NotBeNull();
    }

    [Fact]
    public void Create_Should_CreateValidCommand()
    {
        // Act
        var command = McpCommand.Create(_mockRedmineService, _mockLogger, _mockMcpServerLogger);

        // Assert - command should have proper structure
        command.Name.Should().Be("mcp");
        command.Options.Should().HaveCount(1);
        var debugOption = command.Options.First();
        debugOption.Name.Should().Be("--debug");
    }
}
