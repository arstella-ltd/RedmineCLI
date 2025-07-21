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

    public void FormatIssueDetails(Issue issue)
    {
        // Basic issue details
        var panel = new Panel(new Markup($"[bold]{Markup.Escape(issue.Subject)}[/]"))
        {
            Header = new PanelHeader($"Issue #{issue.Id}"),
            Padding = new Padding(1, 1)
        };
        AnsiConsole.Write(panel);

        AnsiConsole.WriteLine();

        // Issue properties
        var grid = new Grid();
        grid.AddColumn();
        grid.AddColumn();

        grid.AddRow("[bold]Status:[/]", Markup.Escape(issue.Status?.Name ?? "Unknown"));
        grid.AddRow("[bold]Priority:[/]", Markup.Escape(issue.Priority?.Name ?? "Normal"));
        grid.AddRow("[bold]Assignee:[/]", Markup.Escape(issue.AssignedTo?.Name ?? "Unassigned"));
        grid.AddRow("[bold]Project:[/]", Markup.Escape(issue.Project?.Name ?? "No Project"));
        grid.AddRow("[bold]Progress:[/]", $"{issue.DoneRatio ?? 0}%");
        grid.AddRow("[bold]Created:[/]", issue.CreatedOn.ToString("yyyy-MM-dd HH:mm"));
        grid.AddRow("[bold]Updated:[/]", issue.UpdatedOn.ToString("yyyy-MM-dd HH:mm"));

        AnsiConsole.Write(grid);
        AnsiConsole.WriteLine();

        // Description
        if (!string.IsNullOrWhiteSpace(issue.Description))
        {
            AnsiConsole.MarkupLine("[bold]Description:[/]");
            AnsiConsole.WriteLine(Markup.Escape(issue.Description));
            AnsiConsole.WriteLine();
        }

        // History/Journals
        if (issue.Journals != null && issue.Journals.Count > 0)
        {
            AnsiConsole.MarkupLine("[bold]History:[/]");
            foreach (var journal in issue.Journals.OrderBy(j => j.CreatedOn))
            {
                AnsiConsole.WriteLine();
                AnsiConsole.MarkupLine($"[grey]#{journal.Id} - {Markup.Escape(journal.User?.Name ?? "Unknown")} - {journal.CreatedOn:yyyy-MM-dd HH:mm}[/]");

                // Show changes
                if (journal.Details != null && journal.Details.Count > 0)
                {
                    foreach (var detail in journal.Details)
                    {
                        if (detail.Property == "attr")
                        {
                            var oldValue = Markup.Escape(detail.OldValue ?? "");
                            var newValue = Markup.Escape(detail.NewValue ?? "");
                            AnsiConsole.MarkupLine($"  [yellow]Changed {detail.Name} from '{oldValue}' to '{newValue}'[/]");
                        }
                    }
                }

                // Show notes
                if (!string.IsNullOrWhiteSpace(journal.Notes))
                {
                    AnsiConsole.WriteLine($"  {Markup.Escape(journal.Notes)}");
                }
            }
        }
    }
}
