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

        // デバッグ用：HTMLをファイルに保存
        if (_logger.IsEnabled(LogLevel.Debug))
        {
            var debugPath = Path.Combine(Path.GetTempPath(), "redmine_topics_debug.html");
            File.WriteAllText(debugPath, html);
            _logger.LogDebug("Topics HTML saved to: {Path}", debugPath);
        }

        // フォーラムのトピックテーブルを探す
        var topicTable = doc.DocumentNode.SelectSingleNode("//table[@class='list messages']");
        if (topicTable == null)
        {
            _logger.LogDebug("No topic table found with class 'list messages', trying alternative selectors");

            // Alternative selectors for different Redmine themes
            topicTable = doc.DocumentNode.SelectSingleNode("//table[contains(@class,'messages')]") ??
                        doc.DocumentNode.SelectSingleNode("//table[contains(@class,'list')]//tr[contains(@class,'message')]/..");
        }

        if (topicTable == null)
        {
            _logger.LogDebug("No topic table found in HTML");
            return topics;
        }

        var rows = topicTable.SelectNodes(".//tbody/tr") ?? topicTable.SelectNodes(".//tr[position()>1]");
        if (rows == null)
        {
            _logger.LogDebug("No topic rows found in table");
            return topics;
        }

        _logger.LogDebug("Found {Count} topic rows", rows.Count);

        foreach (var row in rows)
        {
            var topic = ParseTopicFromRow(row);
            if (topic != null)
            {
                topics.Add(topic);
                _logger.LogDebug("Parsed topic: ID={Id}, Title={Title}, Author={Author}, Replies={Replies}",
                    topic.Id, topic.Title, topic.Author, topic.Replies);
            }
        }

        return topics;
    }

    public TopicDetail? ParseTopicDetailFromHtml(string html)
    {
        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        var topicDetail = new TopicDetail();

        // 最初のメッセージのIDを取得（トピック自体のID）
        // 最初のメッセージは通常 div[@class='message'] の最初の要素
        var firstMessage = doc.DocumentNode.SelectSingleNode("//div[@class='message'][1]");
        if (firstMessage != null)
        {
            var idAttr = firstMessage.GetAttributeValue("id", "");
            var idMatch = Regex.Match(idAttr, @"message-(\d+)");
            if (idMatch.Success)
            {
                topicDetail.Id = int.Parse(idMatch.Groups[1].Value);
                _logger.LogDebug("Topic ID extracted from message div: {Id}", topicDetail.Id);
            }
        }

        // フォールバック: URLからIDを抽出
        if (topicDetail.Id == 0)
        {
            var currentUrl = doc.DocumentNode.SelectSingleNode("//meta[@property='og:url']")?.GetAttributeValue("content", "");
            if (!string.IsNullOrEmpty(currentUrl))
            {
                var idMatch = Regex.Match(currentUrl, @"/topics/(\d+)|/messages/(\d+)");
                if (idMatch.Success)
                {
                    topicDetail.Id = int.Parse(idMatch.Groups[1].Success ? idMatch.Groups[1].Value : idMatch.Groups[2].Value);
                    _logger.LogDebug("Topic ID extracted from URL: {Id}", topicDetail.Id);
                }
            }
        }

        // タイトル
        var titleNode = doc.DocumentNode.SelectSingleNode("//h2") ??
                       doc.DocumentNode.SelectSingleNode("//div[@class='subject']/h3");
        if (titleNode != null)
        {
            topicDetail.Title = titleNode.InnerText.Trim();
        }

        // 作成者と内容
        // 最初のメッセージを探す（ただし返信ではないもの）
        var messageNode = doc.DocumentNode.SelectSingleNode("//div[@id='content']//div[@class='message' and not(contains(@class,'reply'))]") ??
                         doc.DocumentNode.SelectSingleNode("//div[@class='message' and not(contains(@class,'reply'))][1]");
        if (messageNode != null)
        {
            // h4.reply-header形式で作成者を探す（Board拡張の場合）
            var headerNode = messageNode.SelectSingleNode(".//h4[contains(@class,'header')]");
            if (headerNode != null)
            {
                var userLink = headerNode.SelectSingleNode(".//a[@class='user' or contains(@class,'user ')]");
                if (userLink != null)
                {
                    topicDetail.Author = userLink.InnerText.Trim();
                    _logger.LogDebug("Topic author extracted from header: {Author}", topicDetail.Author);
                }

                // 日付を取得 - ヘッダーテキストから抽出
                var headerText = headerNode.InnerText;
                _logger.LogDebug("Header text for date extraction: {Text}", headerText);

                // 相対時間のパターンを先に試す（より具体的なため）
                var relativePatterns = new[]
                {
                    @"(\d+)\s*(日|時間|分)前",                               // X日前
                    @"about\s+(\d+)\s+(days?|months?|years?|hours?|minutes?)\s+ago" // about X days ago
                };

                bool dateFound = false;
                foreach (var pattern in relativePatterns)
                {
                    var match = Regex.Match(headerText, pattern);
                    if (match.Success)
                    {
                        var amount = int.Parse(match.Groups[1].Value);
                        if (amount < 100) // Sanity check to avoid unrealistic values
                        {
                            var unit = match.Groups[2].Value;
                            topicDetail.CreatedAt = unit.Contains("年") || unit.Contains("year") ? DateTime.Now.AddYears(-amount) :
                                                   unit.Contains("月") || unit.Contains("month") ? DateTime.Now.AddMonths(-amount) :
                                                   unit.Contains("日") || unit.Contains("day") ? DateTime.Now.AddDays(-amount) :
                                                   unit.Contains("時") || unit.Contains("hour") ? DateTime.Now.AddHours(-amount) :
                                                   DateTime.Now.AddMinutes(-amount);
                            _logger.LogDebug("Relative date extracted: {Amount} {Unit} -> {Date}", amount, unit, topicDetail.CreatedAt);
                            dateFound = true;
                            break;
                        }
                    }
                }

                // 相対時間が見つからない場合のみ、絶対日付を探す
                if (!dateFound)
                {
                    // より厳密な日付パターン（時刻付き日付のみ）
                    var absoluteDatePatterns = new[]
                    {
                        @"(\d{4}[-/]\d{1,2}[-/]\d{1,2}\s+\d{1,2}:\d{2})",  // 2024-01-01 12:00
                        @"(\d{1,2}[-/]\d{1,2}[-/]\d{4}\s+\d{1,2}:\d{2})"   // 01-01-2024 12:00
                    };

                    foreach (var pattern in absoluteDatePatterns)
                    {
                        var match = Regex.Match(headerText, pattern);
                        if (match.Success && DateTime.TryParse(match.Groups[1].Value, out var createdAt))
                        {
                            topicDetail.CreatedAt = createdAt;
                            _logger.LogDebug("Absolute date extracted: {Date}", createdAt);
                            dateFound = true;
                            break;
                        }
                    }
                }
            }

            // フォールバック: 従来の方法でauthorを探す
            if (string.IsNullOrEmpty(topicDetail.Author))
            {
                var authorNode = messageNode.SelectSingleNode(".//p[@class='author']") ??
                               messageNode.SelectSingleNode(".//span[@class='author']") ??
                               messageNode.SelectSingleNode(".//div[@class='author']");
                if (authorNode != null)
                {
                    var authorText = authorNode.InnerText.Trim();
                    _logger.LogDebug("Author node text (fallback): {Text}", authorText);

                    // Try to find a link with the author name
                    var authorLink = authorNode.SelectSingleNode(".//a[contains(@href,'/users/')]");
                    if (authorLink != null)
                    {
                        topicDetail.Author = authorLink.InnerText.Trim();
                        _logger.LogDebug("Author extracted from link: {Author}", topicDetail.Author);
                    }
                    else
                    {
                        // Try various patterns
                        var patterns = new[]
                        {
                            @"Added by (.+?) (?:about|over|\d)",  // "Added by Name about..."
                            @"^(.+?),\s*\d",                       // "Name, 2024-01-01..."
                            @"^(.+?)$"                              // Just the name (fallback)
                        };

                        foreach (var pattern in patterns)
                        {
                            var match = Regex.Match(authorText, pattern);
                            if (match.Success && !match.Groups[1].Value.Contains("2025"))
                            {
                                topicDetail.Author = match.Groups[1].Value.Trim();
                                _logger.LogDebug("Author extracted with pattern '{Pattern}': {Author}", pattern, topicDetail.Author);
                                break;
                            }
                        }
                    }

                    // Extract creation date only if not already set
                    if (topicDetail.CreatedAt == default)
                    {
                        // Try relative date patterns first
                        var agoMatch = Regex.Match(authorText, @"(\d+)\s+(days?|months?|years?|hours?|minutes?)\s+ago");
                        if (agoMatch.Success)
                        {
                            var amount = int.Parse(agoMatch.Groups[1].Value);
                            if (amount < 100) // Sanity check
                            {
                                var unit = agoMatch.Groups[2].Value;
                                topicDetail.CreatedAt = unit.StartsWith("year") ? DateTime.Now.AddYears(-amount) :
                                                      unit.StartsWith("month") ? DateTime.Now.AddMonths(-amount) :
                                                      unit.StartsWith("day") ? DateTime.Now.AddDays(-amount) :
                                                      unit.StartsWith("hour") ? DateTime.Now.AddHours(-amount) :
                                                      DateTime.Now.AddMinutes(-amount);
                            }
                        }
                        else
                        {
                            // Try absolute date with time only
                            var dateMatch = Regex.Match(authorText, @"(\d{4}[-/]\d{2}[-/]\d{2}\s+\d{2}:\d{2})|(\d{2}[-/]\d{2}[-/]\d{4}\s+\d{2}:\d{2})");
                            if (dateMatch.Success && DateTime.TryParse(dateMatch.Value, out var createdAt))
                            {
                                topicDetail.CreatedAt = createdAt;
                            }
                        }
                    }
                }
            }

            // コンテンツを取得
            var contentNode = messageNode.SelectSingleNode(".//div[@class='wiki']");
            if (contentNode != null)
            {
                topicDetail.Content = contentNode.InnerText.Trim();
            }
        }

        // 返信を取得
        var replyNodes = doc.DocumentNode.SelectNodes("//div[@id='replies']//div[@class='message reply']") ??
                        doc.DocumentNode.SelectNodes("//div[@class='message reply']");
        if (replyNodes != null)
        {
            foreach (var replyNode in replyNodes)
            {
                var reply = new TopicReply();

                // 返信のIDを取得（id="message-19466" から 19466 を抽出）
                var replyId = replyNode.GetAttributeValue("id", "");
                var replyIdMatch = Regex.Match(replyId, @"message-(\d+)");
                if (replyIdMatch.Success)
                {
                    reply.Id = int.Parse(replyIdMatch.Groups[1].Value);
                    _logger.LogDebug("Reply ID extracted: {Id}", reply.Id);
                }

                // 返信の作成者を取得（h4.reply-header内のa.userから、または従来の方法）
                var replyHeaderNode = replyNode.SelectSingleNode(".//h4[@class='reply-header']");
                if (replyHeaderNode != null)
                {
                    var userLink = replyHeaderNode.SelectSingleNode(".//a[@class='user' or contains(@class,'user ')]");
                    if (userLink != null)
                    {
                        reply.Author = userLink.InnerText.Trim();
                        _logger.LogDebug("Reply author extracted from header: {Author}", reply.Author);
                    }

                    // 日付を取得（相対時間を優先）
                    var headerText = replyHeaderNode.InnerText;
                    
                    // 相対時間のパターン
                    var dateMatch = Regex.Match(headerText, @"(\d+)\s*(日|時間|分)前");
                    if (dateMatch.Success)
                    {
                        var amount = int.Parse(dateMatch.Groups[1].Value);
                        if (amount < 100) // Sanity check
                        {
                            var unit = dateMatch.Groups[2].Value;
                            reply.CreatedAt = unit == "日" ? DateTime.Now.AddDays(-amount) :
                                            unit == "時間" ? DateTime.Now.AddHours(-amount) :
                                            DateTime.Now.AddMinutes(-amount);
                        }
                    }
                    else
                    {
                        // 絶対日付（時刻付きのみ）
                        var absoluteDateMatch = Regex.Match(headerText, @"(\d{4}[-/]\d{2}[-/]\d{2}\s+\d{2}:\d{2})|(\d{2}[-/]\d{2}[-/]\d{4}\s+\d{2}:\d{2})");
                        if (absoluteDateMatch.Success && DateTime.TryParse(absoluteDateMatch.Value, out var createdAt))
                        {
                            reply.CreatedAt = createdAt;
                        }
                    }
                }
                else
                {
                    // フォールバック: 従来の方法
                    var replyAuthorNode = replyNode.SelectSingleNode(".//p[@class='author']") ??
                                         replyNode.SelectSingleNode(".//span[@class='author']");
                    if (replyAuthorNode != null)
                    {
                        var authorText = replyAuthorNode.InnerText.Trim();
                        
                        // Try to extract author name
                        var authorLink = replyAuthorNode.SelectSingleNode(".//a[contains(@href,'/users/')]");
                        if (authorLink != null)
                        {
                            reply.Author = authorLink.InnerText.Trim();
                        }
                        else
                        {
                            // Extract author without including dates that look like content
                            var authorMatch = Regex.Match(authorText, @"^([^,\d]+)");
                            if (authorMatch.Success)
                            {
                                reply.Author = authorMatch.Groups[1].Value.Trim();
                            }
                        }

                        // Extract date (relative first, then absolute with time)
                        var agoMatch = Regex.Match(authorText, @"(\d+)\s+(days?|months?|years?|hours?|minutes?)\s+ago");
                        if (agoMatch.Success)
                        {
                            var amount = int.Parse(agoMatch.Groups[1].Value);
                            if (amount < 100) // Sanity check
                            {
                                var unit = agoMatch.Groups[2].Value;
                                reply.CreatedAt = unit.StartsWith("year") ? DateTime.Now.AddYears(-amount) :
                                                unit.StartsWith("month") ? DateTime.Now.AddMonths(-amount) :
                                                unit.StartsWith("day") ? DateTime.Now.AddDays(-amount) :
                                                unit.StartsWith("hour") ? DateTime.Now.AddHours(-amount) :
                                                DateTime.Now.AddMinutes(-amount);
                            }
                        }
                        else
                        {
                            var dateMatch = Regex.Match(authorText, @"(\d{4}[-/]\d{2}[-/]\d{2}\s+\d{2}:\d{2})|(\d{2}[-/]\d{2}[-/]\d{4}\s+\d{2}:\d{2})");
                            if (dateMatch.Success && DateTime.TryParse(dateMatch.Value, out var createdAt))
                            {
                                reply.CreatedAt = createdAt;
                            }
                        }
                    }
                }

                // 返信の内容を取得
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

            // デバッグ用：行のHTMLを出力
            _logger.LogDebug("Parsing row HTML: {Html}", row.OuterHtml.Substring(0, Math.Min(row.OuterHtml.Length, 500)));

            // タイトルとURL（複数のパターンに対応）
            var subjectCell = row.SelectSingleNode(".//td[@class='subject']") ??
                             row.SelectSingleNode(".//td[contains(@class,'subject')]");

            if (subjectCell == null)
            {
                _logger.LogDebug("No subject cell found in row");
                return null;
            }

            var linkNode = subjectCell.SelectSingleNode(".//a[contains(@href,'/boards/') or contains(@href,'/messages/')]");
            if (linkNode == null)
            {
                _logger.LogDebug("No link found in subject cell");
                return null;
            }

            topic.Title = linkNode.InnerText.Trim();
            var href = linkNode.GetAttributeValue("href", "");
            _logger.LogDebug("Found link with href: {Href}", href);

            // IDを抽出（複数パターンに対応）
            var match = Regex.Match(href, @"/messages/(\d+)");
            if (!match.Success)
            {
                // Alternative pattern for boards/XX/topics/YY
                match = Regex.Match(href, @"/topics/(\d+)");
            }
            if (!match.Success)
            {
                // Try to extract from data attributes or other sources
                var dataId = row.GetAttributeValue("data-message-id", "") ??
                            row.GetAttributeValue("data-id", "");
                if (!string.IsNullOrEmpty(dataId) && int.TryParse(dataId, out var idFromData))
                {
                    topic.Id = idFromData;
                }
                else
                {
                    // Use board-specific ID extraction
                    match = Regex.Match(href, @"/(\d+)(?:[?#]|$)");
                    if (match.Success)
                    {
                        topic.Id = int.Parse(match.Groups[1].Value);
                    }
                }
            }
            else
            {
                topic.Id = int.Parse(match.Groups[1].Value);
            }

            // 完全なURLを構築（相対URLの場合）
            topic.Url = href;

            // スティッキーとロックの状態
            if (subjectCell.InnerHtml.Contains("sticky") || row.GetAttributeValue("class", "").Contains("sticky"))
            {
                topic.IsSticky = true;
            }
            if (subjectCell.InnerHtml.Contains("locked") || row.GetAttributeValue("class", "").Contains("locked"))
            {
                topic.IsLocked = true;
            }

            // 作成者（複数パターンに対応）
            var authorCell = row.SelectSingleNode(".//td[@class='author']") ??
                            row.SelectSingleNode(".//td[contains(@class,'author')]") ??
                            row.SelectSingleNode(".//td[@class='created_by']");
            if (authorCell != null)
            {
                // リンク内のテキストまたはセル直下のテキスト
                var authorLink = authorCell.SelectSingleNode(".//a");
                topic.Author = authorLink != null ? authorLink.InnerText.Trim() : authorCell.InnerText.Trim();
            }

            // 返信数（複数パターンに対応）
            var repliesCell = row.SelectSingleNode(".//td[@class='replies']") ??
                             row.SelectSingleNode(".//td[contains(@class,'replies')]") ??
                             row.SelectSingleNode(".//td[@class='reply-count']") ??
                             row.SelectSingleNode(".//td[@class='comments']");

            if (repliesCell != null)
            {
                var repliesText = repliesCell.InnerText.Trim();
                // 数字のみを抽出
                var numberMatch = Regex.Match(repliesText, @"\d+");
                if (numberMatch.Success && int.TryParse(numberMatch.Value, out var replies))
                {
                    topic.Replies = replies;
                }
            }

            // 最終返信日時（複数パターンに対応）
            var lastReplyCell = row.SelectSingleNode(".//td[@class='last-reply']") ??
                               row.SelectSingleNode(".//td[contains(@class,'last-reply')]") ??
                               row.SelectSingleNode(".//td[@class='last_message']") ??
                               row.SelectSingleNode(".//td[@class='updated_on']");

            if (lastReplyCell != null)
            {
                var dateText = lastReplyCell.InnerText.Trim();
                // 日付部分のみを抽出（「by User」などを除外）
                var dateMatch = Regex.Match(dateText, @"(\d{4}[-/]\d{2}[-/]\d{2}(?:\s+\d{2}:\d{2})?)|(\d{2}[-/]\d{2}[-/]\d{4}(?:\s+\d{2}:\d{2})?)|(\d+\s+(?:days?|hours?|minutes?)\s+ago)");
                if (dateMatch.Success)
                {
                    dateText = dateMatch.Value;
                    if (dateText.Contains("ago"))
                    {
                        // "X days ago" format handling
                        var agoMatch = Regex.Match(dateText, @"(\d+)\s+(days?|hours?|minutes?)");
                        if (agoMatch.Success)
                        {
                            var amount = int.Parse(agoMatch.Groups[1].Value);
                            var unit = agoMatch.Groups[2].Value;
                            topic.LastReply = unit.StartsWith("day") ? DateTime.Now.AddDays(-amount) :
                                            unit.StartsWith("hour") ? DateTime.Now.AddHours(-amount) :
                                            DateTime.Now.AddMinutes(-amount);
                        }
                    }
                    else if (DateTime.TryParse(dateText, out var lastReply))
                    {
                        topic.LastReply = lastReply;
                    }
                }
            }

            _logger.LogDebug("Successfully parsed topic: ID={Id}, Title={Title}, Author={Author}, Replies={Replies}",
                topic.Id, topic.Title, topic.Author, topic.Replies);

            return topic;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing topic from row");
            return null;
        }
    }
}
