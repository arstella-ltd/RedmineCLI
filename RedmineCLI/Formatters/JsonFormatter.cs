using System.Diagnostics.CodeAnalysis;
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

    public void FormatUsers(List<User> users, bool showAllDetails = false)
    {
        // For JSON output, we always serialize the full user object
        // The showAllDetails parameter is preserved for API consistency with TableFormatter
        // Clients consuming JSON can filter fields as needed
        var json = JsonSerializer.Serialize(users, RedmineJsonContext.Default.ListUser);
        AnsiConsole.WriteLine(json);
    }

    public void FormatProjects(List<Project> projects)
    {
        var json = JsonSerializer.Serialize(projects, RedmineJsonContext.Default.ListProject);
        AnsiConsole.WriteLine(json);
    }

    public void FormatIssueStatuses(List<IssueStatus> statuses)
    {
        var json = JsonSerializer.Serialize(statuses, RedmineJsonContext.Default.ListIssueStatus);
        AnsiConsole.WriteLine(json);
    }

    public void FormatPriorities(List<Priority> priorities)
    {
        var json = JsonSerializer.Serialize(priorities, RedmineJsonContext.Default.ListPriority);
        AnsiConsole.WriteLine(json);
    }
}
