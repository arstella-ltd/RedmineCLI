using System.Text.Json.Serialization;

namespace RedmineCLI.Extension.Board.Models;

/// <summary>
/// Represents a Redmine board
/// </summary>
public class Board
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ProjectName { get; set; }
    public int? ProjectId { get; set; }
    public string Url { get; set; } = string.Empty;
    public int ColumnCount { get; set; }
    public int CardCount { get; set; }
}

public class Topic
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public int Replies { get; set; }
    public DateTime? LastReply { get; set; }
    public string Url { get; set; } = string.Empty;
    public bool IsSticky { get; set; }
    public bool IsLocked { get; set; }
}

public class TopicDetail : Topic
{
    public string Content { get; set; } = string.Empty;
    public new List<TopicReply> Replies { get; set; } = new();
}

public class TopicReply
{
    public int Id { get; set; }
    public string Author { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string Content { get; set; } = string.Empty;
}

/// <summary>
/// Represents a board column
/// </summary>
public class BoardColumn
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Position { get; set; }
    public int CardCount { get; set; }
}

/// <summary>
/// Represents a board card
/// </summary>
public class BoardCard
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? AssignedTo { get; set; }
    public string? Status { get; set; }
    public string? Priority { get; set; }
    public int? IssueId { get; set; }
}

/// <summary>
/// JSON serialization context for AOT support
/// </summary>
[JsonSerializable(typeof(Board))]
[JsonSerializable(typeof(BoardColumn))]
[JsonSerializable(typeof(BoardCard))]
[JsonSerializable(typeof(List<Board>))]
[JsonSerializable(typeof(List<BoardColumn>))]
[JsonSerializable(typeof(List<BoardCard>))]
public partial class BoardJsonContext : JsonSerializerContext
{
}
