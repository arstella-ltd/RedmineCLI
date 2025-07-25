using System.CommandLine;
using System.IO;

using FluentAssertions;

using RedmineCLI.Commands;
using RedmineCLI.Models;

using Xunit;

namespace RedmineCLI.Tests.Commands;

public class LlmsCommandTests
{
    [Fact]
    public void Create_Should_ReturnValidCommand()
    {
        // Act
        var command = LlmsCommand.Create();

        // Assert
        command.Should().NotBeNull();
        command.Name.Should().Be("llms");
        command.Description.Should().Contain("Output detailed information about all commands");
    }

    [Fact]
    public void Create_Should_SetupCommandProperly()
    {
        // Arrange & Act
        var command = LlmsCommand.Create();

        // Assert
        command.Should().NotBeNull();
        command.Name.Should().Be("llms");
        command.Description.Should().NotBeNullOrEmpty();
        command.Subcommands.Should().BeEmpty();
        command.Options.Should().BeEmpty();
    }

}