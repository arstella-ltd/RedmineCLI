using System.Text.RegularExpressions;

using RedmineCLI.Models;

namespace RedmineCLI.Utils;

public partial class ImageReferenceDetector
{
    // Markdown形式の画像参照: ![alt text](filename)
    [GeneratedRegex(@"!\[.*?\]\(([^)]+)\)", RegexOptions.Compiled)]
    private static partial Regex MarkdownImageRegex();

    // Redmine Wiki形式の画像参照: {{thumbnail(filename)}} または {{image(filename)}}
    [GeneratedRegex(@"{{\s*(?:thumbnail|image)\s*\(([^)]+)\)\s*}}", RegexOptions.Compiled)]
    private static partial Regex RedmineImageRegex();

    public List<string> DetectImageReferences(string? description)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            return new List<string>();
        }

        var references = new HashSet<string>();

        // Markdown形式の画像を検出
        var markdownMatches = MarkdownImageRegex().Matches(description);
        foreach (Match match in markdownMatches)
        {
            if (match.Groups.Count > 1)
            {
                references.Add(match.Groups[1].Value.Trim());
            }
        }

        // Redmine Wiki形式の画像を検出
        var redmineMatches = RedmineImageRegex().Matches(description);
        foreach (Match match in redmineMatches)
        {
            if (match.Groups.Count > 1)
            {
                references.Add(match.Groups[1].Value.Trim());
            }
        }

        return references.ToList();
    }

    public List<Attachment> FindMatchingAttachments(List<Attachment>? attachments, List<string> imageReferences)
    {
        if (attachments == null || imageReferences == null || imageReferences.Count == 0)
        {
            return new List<Attachment>();
        }

        return attachments
            .Where(a => imageReferences.Contains(a.Filename) && IsImageContentType(a.ContentType))
            .ToList();
    }

    public bool IsImageContentType(string? contentType)
    {
        if (string.IsNullOrWhiteSpace(contentType))
        {
            return false;
        }

        return contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase);
    }
}