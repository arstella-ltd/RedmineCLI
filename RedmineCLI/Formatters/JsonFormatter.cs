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

    public void FormatIssueDetails(Issue issue)
    {
        var json = JsonSerializer.Serialize(issue, RedmineJsonContext.Default.Issue);
        AnsiConsole.WriteLine(json);
    }
}
