using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using RedmineCLI.ApiClient;
using RedmineCLI.Models;
using Spectre.Console;

namespace RedmineCLI.Utils
{
    /// <summary>
    /// テキスト内の画像参照を実際の画像に置き換えて表示するクラス
    /// </summary>
    public class InlineImageRenderer
    {
        private readonly IRedmineApiClient? _apiClient;
        private readonly ImageReferenceDetector _imageDetector;
        
        // Markdown形式の画像参照: ![alt text](filename)
        private static readonly Regex MarkdownImageRegex = new Regex(@"!\[.*?\]\(([^)]+)\)", RegexOptions.Compiled);
        
        // Redmine Wiki形式の画像参照: {{thumbnail(filename)}} または {{image(filename)}}
        private static readonly Regex RedmineImageRegex = new Regex(@"{{\s*(?:thumbnail|image)\s*\(([^)]+)\)\s*}}", RegexOptions.Compiled);

        public InlineImageRenderer(IRedmineApiClient? apiClient)
        {
            _apiClient = apiClient;
            _imageDetector = new ImageReferenceDetector();
        }

        /// <summary>
        /// テキスト内の画像参照を処理して表示
        /// </summary>
        public void RenderTextWithInlineImages(string? text, List<Attachment>? attachments, bool showImages)
        {
            if (string.IsNullOrWhiteSpace(text) || attachments == null || attachments.Count == 0)
            {
                if (!string.IsNullOrWhiteSpace(text))
                {
                    AnsiConsole.WriteLine(text);
                }
                return;
            }

            // 画像参照とその位置を検出
            var imagePositions = new List<(int start, int end, string filename, bool isThumbnail)>();
            
            // Markdown形式
            var markdownMatches = MarkdownImageRegex.Matches(text);
            foreach (Match match in markdownMatches)
            {
                if (match.Groups.Count > 1)
                {
                    imagePositions.Add((match.Index, match.Index + match.Length, match.Groups[1].Value.Trim(), false));
                }
            }
            
            // Redmine Wiki形式
            var redmineMatches = RedmineImageRegex.Matches(text);
            foreach (Match match in redmineMatches)
            {
                if (match.Groups.Count > 1)
                {
                    var isThumbnail = match.Value.Contains("thumbnail");
                    imagePositions.Add((match.Index, match.Index + match.Length, match.Groups[1].Value.Trim(), isThumbnail));
                }
            }
            
            // 位置でソート
            imagePositions.Sort((a, b) => a.start.CompareTo(b.start));
            
            // テキストを分割して表示
            int lastEnd = 0;
            foreach (var (start, end, filename, isThumbnail) in imagePositions)
            {
                // 画像参照の前のテキストを表示
                if (start > lastEnd)
                {
                    var beforeText = text.Substring(lastEnd, start - lastEnd);
                    AnsiConsole.Write(beforeText);
                }
                
                // 画像参照を色付きで表示
                var imageRef = text.Substring(start, end - start);
                AnsiConsole.Markup($"[cyan]{Markup.Escape(imageRef)}[/]");
                
                // 画像を表示（showImagesがtrueで、対応する添付ファイルがある場合）
                if (showImages && TerminalCapabilityDetector.SupportsSixel())
                {
                    var attachment = attachments.FirstOrDefault(a => 
                        a.Filename == filename && _imageDetector.IsImageContentType(a.ContentType));
                    
                    if (attachment != null)
                    {
                        AnsiConsole.WriteLine(); // 改行
                        RenderImage(attachment, isThumbnail);
                    }
                }
                
                lastEnd = end;
            }
            
            // 残りのテキストを表示
            if (lastEnd < text.Length)
            {
                AnsiConsole.Write(text.Substring(lastEnd));
            }
            
            AnsiConsole.WriteLine(); // 最後に改行
        }
        
        private void RenderImage(Attachment attachment, bool isThumbnail)
        {
            if (_apiClient == null)
                return;
                
            var httpClient = (_apiClient as RedmineApiClient)?.GetHttpClient();
            var apiKey = (_apiClient as RedmineApiClient)?.GetApiKey();
            
            if (httpClient != null)
            {
                // サムネイルの場合は小さめに表示
                int maxWidth = isThumbnail ? 100 : 200;
                
                SixelImageRenderer.RenderActualImage(
                    attachment.ContentUrl, 
                    httpClient, 
                    apiKey, 
                    attachment.Filename,
                    maxWidth
                );
            }
        }
    }
}