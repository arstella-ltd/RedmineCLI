using RedmineCLI.Models;
using RedmineCLI.Utils;

using Spectre.Console;

namespace RedmineCLI.Formatters;

public class TableFormatter : ITableFormatter
{
    private readonly ITimeHelper _timeHelper;
    private TimeFormat _timeFormat = TimeFormat.Relative;

    public TableFormatter(ITimeHelper timeHelper)
    {
        _timeHelper = timeHelper;
    }

    public void SetTimeFormat(TimeFormat format)
    {
        _timeFormat = format;
    }

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
        table.AddColumn("Priority");
        table.AddColumn("Status");
        table.AddColumn("Assignee");
        table.AddColumn("Project");
        table.AddColumn("Due Date");
        table.AddColumn("Updated");

        foreach (var issue in issues)
        {
            table.AddRow(
                issue.Id.ToString(),
                Markup.Escape(issue.Subject ?? string.Empty),
                Markup.Escape(issue.Priority?.Name ?? "Normal"),
                Markup.Escape(issue.Status?.Name ?? "Unknown"),
                Markup.Escape(issue.AssignedTo?.Name ?? "Unassigned"),
                Markup.Escape(issue.Project?.Name ?? "No Project"),
                issue.DueDate.HasValue ? _timeHelper.GetLocalTime(issue.DueDate.Value, "yyyy-MM-dd") : "Not set",
                _timeHelper.FormatTime(issue.UpdatedOn, _timeFormat)
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
        grid.AddRow("[bold]Due Date:[/]", issue.DueDate.HasValue ? _timeHelper.GetLocalTime(issue.DueDate.Value, "yyyy-MM-dd") : "Not set");
        grid.AddRow("[bold]Created:[/]", _timeHelper.FormatTime(issue.CreatedOn, _timeFormat));
        grid.AddRow("[bold]Updated:[/]", _timeHelper.FormatTime(issue.UpdatedOn, _timeFormat));

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
                AnsiConsole.MarkupLine($"[grey]#{journal.Id} - {Markup.Escape(journal.User?.Name ?? "Unknown")} - {_timeHelper.FormatTime(journal.CreatedOn, _timeFormat)}[/]");

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
            AnsiConsole.WriteLine();
        }

        // Attachments
        if (issue.Attachments != null && issue.Attachments.Count > 0)
        {
            AnsiConsole.MarkupLine("[bold]Attachments:[/]");

            var attachmentTable = new Table();
            attachmentTable.AddColumn("ID");
            attachmentTable.AddColumn("Filename");
            attachmentTable.AddColumn("Size");
            attachmentTable.AddColumn("Author");
            attachmentTable.AddColumn("Created");

            foreach (var attachment in issue.Attachments)
            {
                attachmentTable.AddRow(
                    $"#{attachment.Id}",
                    Markup.Escape(attachment.Filename),
                    FormatFileSize(attachment.Filesize),
                    Markup.Escape(attachment.Author?.Name ?? "Unknown"),
                    _timeHelper.FormatTime(attachment.CreatedOn, _timeFormat)
                );
            }

            AnsiConsole.Write(attachmentTable);
        }
    }

    public void FormatAttachments(List<Attachment> attachments)
    {
        var table = new Table();
        table.AddColumn("ID");
        table.AddColumn("Filename");
        table.AddColumn("Size");
        table.AddColumn("Type");
        table.AddColumn("Author");
        table.AddColumn("Created");

        foreach (var attachment in attachments)
        {
            table.AddRow(
                $"#{attachment.Id}",
                Markup.Escape(attachment.Filename),
                FormatFileSize(attachment.Filesize),
                Markup.Escape(attachment.ContentType),
                Markup.Escape(attachment.Author?.Name ?? "Unknown"),
                _timeHelper.FormatTime(attachment.CreatedOn, _timeFormat)
            );
        }

        AnsiConsole.Write(table);
    }

    public void FormatAttachmentDetails(Attachment attachment)
    {
        var panel = new Panel($"[bold]Attachment #{attachment.Id}[/]")
            .BorderColor(Color.Blue)
            .Header($"[blue]{Markup.Escape(attachment.Filename)}[/]");

        var grid = new Grid()
            .AddColumn()
            .AddColumn();

        grid.AddRow("[bold]Filename:[/]", Markup.Escape(attachment.Filename));
        grid.AddRow("[bold]Size:[/]", FormatFileSize(attachment.Filesize));
        grid.AddRow("[bold]Type:[/]", Markup.Escape(attachment.ContentType));

        if (!string.IsNullOrEmpty(attachment.Description))
        {
            grid.AddRow("[bold]Description:[/]", Markup.Escape(attachment.Description));
        }

        if (attachment.Author != null)
        {
            grid.AddRow("[bold]Author:[/]", Markup.Escape(attachment.Author.Name));
        }

        grid.AddRow("[bold]Created:[/]", _timeHelper.FormatTime(attachment.CreatedOn, _timeFormat));
        grid.AddRow("[bold]URL:[/]", Markup.Escape(attachment.ContentUrl));

        AnsiConsole.Write(panel);
        AnsiConsole.WriteLine();
        AnsiConsole.Write(grid);
    }

    private static string FormatFileSize(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        double size = bytes;
        int order = 0;

        while (size >= 1024 && order < sizes.Length - 1)
        {
            order++;
            size = size / 1024;
        }

        return $"{size:0.##} {sizes[order]}";
    }
}
