using System.Text.RegularExpressions;

using HtmlAgilityPack;

using Microsoft.Extensions.Logging;

using RedmineCLI.Extension.Board.Models;

namespace RedmineCLI.Extension.Board.Services;

/// <summary>
/// HTML解析のためのサービス実装
/// </summary>
public class HtmlParsingService : IHtmlParsingService
{
    private readonly ILogger<HtmlParsingService> _logger;

    public HtmlParsingService(ILogger<HtmlParsingService> logger)
    {
        _logger = logger;
    }

    public List<Models.Board> ParseBoardsFromHtml(string html, string baseUrl)
    {
        var boards = new List<Models.Board>();
        _logger.LogDebug("Parsing HTML for boards (content length: {Length})", html.Length);

        try
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            // テーブル形式のボード一覧を探す（<tr class="board">タグを利用）
            var boardRows = doc.DocumentNode.SelectNodes("//tr[@class='board']");
            if (boardRows == null)
            {
                _logger.LogDebug("No board rows found in HTML");
                return boards;
            }

            _logger.LogDebug("Found {Count} board table rows", boardRows.Count);

            foreach (var row in boardRows)
            {
                var board = ParseBoardFromRow(row, baseUrl);
                if (board != null)
                {
                    boards.Add(board);
                    _logger.LogDebug("Found board: {Name} (ID: {Id}, Topics: {Topics}, Messages: {Messages})",
                        board.Name, board.Id, board.ColumnCount, board.CardCount);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing boards from HTML");
        }

        _logger.LogDebug("Parsed {Count} boards from HTML", boards.Count);
        return boards;
    }

    public List<Topic> ParseTopicsFromHtml(string html)
    {
        var topics = new List<Topic>();
        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        // フォーラムのトピックテーブルを探す
        var topicTable = doc.DocumentNode.SelectSingleNode("//table[@class='list messages']");
        if (topicTable == null)
        {
            return topics;
        }

        var rows = topicTable.SelectNodes(".//tbody/tr");
        if (rows == null)
        {
            return topics;
        }

        foreach (var row in rows)
        {
            var topic = ParseTopicFromRow(row);
            if (topic != null)
            {
                topics.Add(topic);
            }
        }

        return topics;
    }

    public TopicDetail? ParseTopicDetailFromHtml(string html)
    {
        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        var topicDetail = new TopicDetail();

        // タイトル
        var titleNode = doc.DocumentNode.SelectSingleNode("//h2") ??
                       doc.DocumentNode.SelectSingleNode("//div[@class='subject']/h3");
        if (titleNode != null)
        {
            topicDetail.Title = titleNode.InnerText.Trim();
        }

        // 作成者と内容
        var messageNode = doc.DocumentNode.SelectSingleNode("//div[@id='content']//div[@class='message']");
        if (messageNode != null)
        {
            var authorNode = messageNode.SelectSingleNode(".//p[@class='author']") ??
                           messageNode.SelectSingleNode(".//span[@class='author']");
            if (authorNode != null)
            {
                topicDetail.Author = authorNode.InnerText.Trim();
            }

            var contentNode = messageNode.SelectSingleNode(".//div[@class='wiki']");
            if (contentNode != null)
            {
                topicDetail.Content = contentNode.InnerText.Trim();
            }
        }

        // 返信を取得
        var replyNodes = doc.DocumentNode.SelectNodes("//div[@id='replies']//div[@class='message reply']");
        if (replyNodes != null)
        {
            foreach (var replyNode in replyNodes)
            {
                var reply = new TopicReply();

                var replyAuthorNode = replyNode.SelectSingleNode(".//p[@class='author']") ??
                                     replyNode.SelectSingleNode(".//span[@class='author']");
                if (replyAuthorNode != null)
                {
                    reply.Author = replyAuthorNode.InnerText.Trim();
                }

                var replyContentNode = replyNode.SelectSingleNode(".//div[@class='wiki']");
                if (replyContentNode != null)
                {
                    reply.Content = replyContentNode.InnerText.Trim();
                }

                topicDetail.Replies.Add(reply);
            }
        }

        return topicDetail;
    }

    private Models.Board? ParseBoardFromRow(HtmlNode row, string baseUrl)
    {
        // ボードへのリンクを取得
        var linkNode = row.SelectSingleNode(".//a[contains(@href, '/boards/')]");
        if (linkNode == null)
        {
            _logger.LogDebug("No board link found in row");
            return null;
        }

        var href = linkNode.GetAttributeValue("href", "");
        var boardId = ExtractBoardId(href);
        if (boardId == null)
        {
            _logger.LogDebug("Could not extract board ID from href: {Href}", href);
            return null;
        }

        // ボード名を取得
        var boardName = ExtractBoardName(linkNode, boardId.Value);

        // トピック数とメッセージ数を取得
        var topicCount = ExtractCount(row, "topic-count");
        var messageCount = ExtractCount(row, "message-count");

        return new Models.Board
        {
            Id = boardId.Value,
            Name = System.Net.WebUtility.HtmlDecode(boardName),
            Url = $"{baseUrl}{href}",
            ColumnCount = topicCount,
            CardCount = messageCount
        };
    }

    private int? ExtractBoardId(string href)
    {
        var boardIdMatch = Regex.Match(href, @"/boards/(\d+)");
        if (!boardIdMatch.Success)
            return null;

        if (int.TryParse(boardIdMatch.Groups[1].Value, out var boardId))
            return boardId;

        return null;
    }

    private string ExtractBoardName(HtmlNode linkNode, int boardId)
    {
        var nameNode = linkNode.SelectSingleNode(".//span[@class='icon-label']");
        if (nameNode != null)
            return nameNode.InnerText.Trim();

        return $"Board {boardId}";
    }

    private int ExtractCount(HtmlNode row, string className)
    {
        var node = row.SelectSingleNode($".//td[@class='{className}']");
        if (node == null)
            return 0;

        if (int.TryParse(node.InnerText.Trim(), out var count))
            return count;

        return 0;
    }

    private Topic? ParseTopicFromRow(HtmlNode row)
    {
        try
        {
            var topic = new Topic();

            // タイトルとURL
            var subjectCell = row.SelectSingleNode(".//td[@class='subject']");
            if (subjectCell == null) return null;

            var linkNode = subjectCell.SelectSingleNode(".//a");
            if (linkNode == null) return null;

            topic.Title = linkNode.InnerText.Trim();
            var href = linkNode.GetAttributeValue("href", "");

            // IDを抽出
            var match = Regex.Match(href, @"/messages/(\d+)");
            if (match.Success)
            {
                topic.Id = int.Parse(match.Groups[1].Value);
            }

            // 完全なURLを構築（相対URLの場合）
            if (!href.StartsWith("http"))
            {
                // BaseUrlは既にauth内にあるので、それを使う必要がある
                // ここでは相対URLをそのまま保存
                topic.Url = href;
            }
            else
            {
                topic.Url = href;
            }

            // スティッキーとロックの状態
            if (subjectCell.InnerHtml.Contains("sticky"))
            {
                topic.IsSticky = true;
            }
            if (subjectCell.InnerHtml.Contains("locked"))
            {
                topic.IsLocked = true;
            }

            // 作成者
            var authorCell = row.SelectSingleNode(".//td[@class='author']");
            if (authorCell != null)
            {
                topic.Author = authorCell.InnerText.Trim();
            }

            // 返信数
            var repliesCell = row.SelectSingleNode(".//td[@class='replies']");
            if (repliesCell != null && int.TryParse(repliesCell.InnerText.Trim(), out var replies))
            {
                topic.Replies = replies;
            }

            // 最終返信日時
            var lastReplyCell = row.SelectSingleNode(".//td[@class='last-reply']");
            if (lastReplyCell != null)
            {
                var dateText = lastReplyCell.InnerText.Trim();
                if (DateTime.TryParse(dateText, out var lastReply))
                {
                    topic.LastReply = lastReply;
                }
            }

            return topic;
        }
        catch
        {
            return null;
        }
    }
}
