namespace RedmineCLI.Extension.Board.Parsers;

/// <summary>
/// board:topic記法のパーサー
/// </summary>
public static class BoardTopicParser
{
    /// <summary>
    /// board:topic記法をパースする
    /// </summary>
    /// <param name="input">入力文字列（例: "21", "21:145", "*"）</param>
    /// <returns>パース結果</returns>
    public static BoardTopicParseResult Parse(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return new BoardTopicParseResult { IsValid = false };
        }

        // ワイルドカード対応
        if (input == "*")
        {
            return new BoardTopicParseResult
            {
                IsValid = true,
                IsWildcard = true
            };
        }

        // board:topic形式
        var parts = input.Split(':');

        if (parts.Length == 1)
        {
            // ボードIDのみ
            if (int.TryParse(parts[0], out var boardId))
            {
                return new BoardTopicParseResult
                {
                    IsValid = true,
                    BoardId = boardId
                };
            }
        }
        else if (parts.Length == 2)
        {
            // board:topic形式
            if (int.TryParse(parts[0], out var boardId) &&
                int.TryParse(parts[1], out var topicId))
            {
                return new BoardTopicParseResult
                {
                    IsValid = true,
                    BoardId = boardId,
                    TopicId = topicId
                };
            }
        }

        return new BoardTopicParseResult { IsValid = false };
    }
}

/// <summary>
/// board:topic記法のパース結果
/// </summary>
public class BoardTopicParseResult
{
    public bool IsValid { get; set; }
    public int? BoardId { get; set; }
    public int? TopicId { get; set; }
    public bool IsWildcard { get; set; }
}
