using System.Collections.Generic;
using System.Text.Json;

using FluentAssertions;

using RedmineCLI.Extension.Board.Models;

using Xunit;

using BoardModel = RedmineCLI.Extension.Board.Models.Board;

namespace RedmineCLI.Extension.Board.Tests.Models;

public class BoardModelSerializationTests
{
    [Fact]
    public void BoardCard_ShouldSerializeUsingJsonContext()
    {
        var card = new BoardCard
        {
            Id = 42,
            Title = "Sample Card",
            AssignedTo = "alice",
            Status = "Open",
            Priority = "High",
            IssueId = 99
        };

        var json = JsonSerializer.Serialize(card, BoardJsonContext.Default.BoardCard);

        json.Should().Contain("\"Id\":42");
        json.Should().Contain("Sample Card");
    }

    [Fact]
    public void BoardColumn_ShouldSerializeUsingJsonContext()
    {
        var column = new BoardColumn
        {
            Id = 7,
            Name = "In Progress",
            Position = 2,
            CardCount = 5
        };

        var json = JsonSerializer.Serialize(column, BoardJsonContext.Default.BoardColumn);

        json.Should().Contain("\"Name\":\"In Progress\"");
        json.Should().Contain("\"CardCount\":5");
    }

    [Fact]
    public void BoardList_ShouldSerializeUsingJsonContext()
    {
        var boards = new List<BoardModel>
        {
            new()
            {
                Id = 1,
                Name = "General",
                Description = "Default board",
                ProjectName = "demo",
                ProjectId = 10,
                Url = "https://example.com",
                ColumnCount = 3,
                CardCount = 12
            }
        };

        var json = JsonSerializer.Serialize(boards, BoardJsonContext.Default.ListBoard);

        json.Should().Contain("General");
        json.Should().Contain("\"ProjectId\":10");
    }
}
