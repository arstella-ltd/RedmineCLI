using FluentAssertions;

using RedmineCLI.Extension.Board.Parsers;

namespace RedmineCLI.Extension.Board.Tests.Parsers;

public class BoardTopicParserTests
{
    [Fact]
    public void Parse_Should_ReturnInvalid_When_InputIsNull()
    {
        // Arrange
        string? input = null;

        // Act
        var result = BoardTopicParser.Parse(input!);

        // Assert
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Parse_Should_ReturnInvalid_When_InputIsEmpty()
    {
        // Arrange
        var input = "";

        // Act
        var result = BoardTopicParser.Parse(input);

        // Assert
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Parse_Should_ReturnInvalid_When_InputIsWhitespace()
    {
        // Arrange
        var input = "   ";

        // Act
        var result = BoardTopicParser.Parse(input);

        // Assert
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Parse_Should_ReturnWildcard_When_InputIsAsterisk()
    {
        // Arrange
        var input = "*";

        // Act
        var result = BoardTopicParser.Parse(input);

        // Assert
        result.IsValid.Should().BeTrue();
        result.IsWildcard.Should().BeTrue();
        result.BoardId.Should().BeNull();
        result.TopicId.Should().BeNull();
    }

    [Fact]
    public void Parse_Should_ReturnBoardIdOnly_When_InputIsSingleNumber()
    {
        // Arrange
        var input = "21";

        // Act
        var result = BoardTopicParser.Parse(input);

        // Assert
        result.IsValid.Should().BeTrue();
        result.BoardId.Should().Be(21);
        result.TopicId.Should().BeNull();
        result.IsWildcard.Should().BeFalse();
    }

    [Fact]
    public void Parse_Should_ReturnBoardAndTopicId_When_InputIsBoardColonTopic()
    {
        // Arrange
        var input = "21:145";

        // Act
        var result = BoardTopicParser.Parse(input);

        // Assert
        result.IsValid.Should().BeTrue();
        result.BoardId.Should().Be(21);
        result.TopicId.Should().Be(145);
        result.IsWildcard.Should().BeFalse();
    }

    [Fact]
    public void Parse_Should_ReturnInvalid_When_BoardIdIsNotNumber()
    {
        // Arrange
        var input = "abc";

        // Act
        var result = BoardTopicParser.Parse(input);

        // Assert
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Parse_Should_ReturnInvalid_When_BoardIdIsNotNumber_InBoardColonTopicFormat()
    {
        // Arrange
        var input = "abc:145";

        // Act
        var result = BoardTopicParser.Parse(input);

        // Assert
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Parse_Should_ReturnInvalid_When_TopicIdIsNotNumber_InBoardColonTopicFormat()
    {
        // Arrange
        var input = "21:abc";

        // Act
        var result = BoardTopicParser.Parse(input);

        // Assert
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Parse_Should_ReturnInvalid_When_TooManyColons()
    {
        // Arrange
        var input = "21:145:999";

        // Act
        var result = BoardTopicParser.Parse(input);

        // Assert
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Parse_Should_HandleLargeNumbers()
    {
        // Arrange
        var input = "999999:888888";

        // Act
        var result = BoardTopicParser.Parse(input);

        // Assert
        result.IsValid.Should().BeTrue();
        result.BoardId.Should().Be(999999);
        result.TopicId.Should().Be(888888);
    }

    [Fact]
    public void Parse_Should_ReturnInvalid_When_ColonOnly()
    {
        // Arrange
        var input = ":";

        // Act
        var result = BoardTopicParser.Parse(input);

        // Assert
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Parse_Should_ReturnInvalid_When_EmptyBeforeColon()
    {
        // Arrange
        var input = ":145";

        // Act
        var result = BoardTopicParser.Parse(input);

        // Assert
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Parse_Should_ReturnInvalid_When_EmptyAfterColon()
    {
        // Arrange
        var input = "21:";

        // Act
        var result = BoardTopicParser.Parse(input);

        // Assert
        result.IsValid.Should().BeFalse();
    }
}
