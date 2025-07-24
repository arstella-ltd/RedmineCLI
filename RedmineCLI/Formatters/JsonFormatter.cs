using System.Text.Json;

using RedmineCLI.ApiClient;
using RedmineCLI.Models;

using Spectre.Console;

namespace RedmineCLI.Formatters;

public class JsonFormatter : IJsonFormatter
{
    private readonly JsonSerializerOptions _jsonOptions;

    public JsonFormatter()
    {
        _jsonOptions = new JsonSerializerOptions(RedmineJsonContext.Default.Options);
    }

    public void FormatIssues(List<Issue> issues)
    {
        var json = JsonSerializer.Serialize(issues, RedmineJsonContext.Default.ListIssue);
        AnsiConsole.WriteLine(json);
    }

    public void FormatIssueDetails(Issue issue, bool showImages = false)
    {
        // showImages parameter is ignored for JSON output
        var json = JsonSerializer.Serialize(issue, RedmineJsonContext.Default.Issue);
        AnsiConsole.WriteLine(json);
    }

    public void FormatAttachments(List<Attachment> attachments)
    {
        var json = JsonSerializer.Serialize(attachments, RedmineJsonContext.Default.ListAttachment);
        AnsiConsole.WriteLine(json);
    }

    public void FormatAttachmentDetails(Attachment attachment)
    {
        var json = JsonSerializer.Serialize(attachment, RedmineJsonContext.Default.Attachment);
        AnsiConsole.WriteLine(json);
    }

    public void FormatObject<T>(T obj)
    {
        if (obj is List<Attachment> attachments)
        {
            var json = JsonSerializer.Serialize(attachments, RedmineJsonContext.Default.ListAttachment);
            AnsiConsole.WriteLine(json);
        }
        else
        {
            // For now, just convert to string for unknown types
            AnsiConsole.WriteLine(obj?.ToString() ?? "null");
        }
    }
}
