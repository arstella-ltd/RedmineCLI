using RedmineCLI.Models;
using Spectre.Console;

namespace RedmineCLI.Formatters;

public class TableFormatter : ITableFormatter
{
    public void FormatIssues(List<Issue> issues)
    {
        if (issues.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No issues found.[/]");
            return;
        }

        var table = new Table();
        table.AddColumn("ID");
        table.AddColumn("Subject");
        table.AddColumn("Status");
        table.AddColumn("Assignee");
        table.AddColumn("Project");
        table.AddColumn("Updated");

        foreach (var issue in issues)
        {
            table.AddRow(
                issue.Id.ToString(),
                Markup.Escape(issue.Subject ?? string.Empty),
                Markup.Escape(issue.Status?.Name ?? "Unknown"),
                Markup.Escape(issue.AssignedTo?.Name ?? "Unassigned"),
                Markup.Escape(issue.Project?.Name ?? "No Project"),
                issue.UpdatedOn.ToString("yyyy-MM-dd HH:mm")
            );
        }

        AnsiConsole.Write(table);
    }
}