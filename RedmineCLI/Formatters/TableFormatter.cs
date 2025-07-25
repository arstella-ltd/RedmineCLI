using RedmineCLI.ApiClient;
using RedmineCLI.Models;
using RedmineCLI.Utils;

using Spectre.Console;

namespace RedmineCLI.Formatters;

public class TableFormatter : ITableFormatter
{
    private readonly ITimeHelper _timeHelper;
    private readonly IRedmineApiClient? _apiClient;
    private readonly ImageReferenceDetector _imageDetector;
    private readonly InlineImageRenderer _inlineImageRenderer;
    private TimeFormat _timeFormat = TimeFormat.Relative;

    public TableFormatter(ITimeHelper timeHelper, IRedmineApiClient? apiClient = null)
    {
        _timeHelper = timeHelper;
        _apiClient = apiClient;
        _imageDetector = new ImageReferenceDetector();
        _inlineImageRenderer = new InlineImageRenderer(_apiClient);
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

        // Check if any issue has a SearchResultType (indicates search results)
        bool hasSearchResultType = issues.Any(i => !string.IsNullOrEmpty(i.SearchResultType));

        table.AddColumn("ID");
        if (hasSearchResultType)
        {
            table.AddColumn("Type");
        }
        table.AddColumn("Subject");
        table.AddColumn("Priority");
        table.AddColumn("Status");
        table.AddColumn("Assignee");
        table.AddColumn("Project");
        table.AddColumn("Due Date");
        table.AddColumn("Updated");

        foreach (var issue in issues)
        {
            if (hasSearchResultType)
            {
                table.AddRow(
                    issue.Id.ToString(),
                    Markup.Escape(issue.SearchResultType ?? "issue"),
                    Markup.Escape(issue.Subject ?? string.Empty),
                    Markup.Escape(issue.Priority?.Name ?? "Normal"),
                    Markup.Escape(issue.Status?.Name ?? "Unknown"),
                    Markup.Escape(issue.AssignedTo?.Name ?? "Unassigned"),
                    Markup.Escape(issue.Project?.Name ?? "No Project"),
                    issue.DueDate.HasValue ? _timeHelper.GetLocalTime(issue.DueDate.Value, "yyyy-MM-dd") : "Not set",
                    _timeHelper.FormatTime(issue.UpdatedOn, _timeFormat)
                );
            }
            else
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
        }

        AnsiConsole.Write(table);
    }

    public void FormatIssueDetails(Issue issue)
    {
        FormatIssueDetails(issue, false);
    }

    public void FormatIssueDetails(Issue issue, bool showImages)
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
            _inlineImageRenderer.RenderTextWithInlineImages(issue.Description, issue.Attachments, showImages);
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
                    AnsiConsole.Write("  ");
                    _inlineImageRenderer.RenderTextWithInlineImages(journal.Notes, issue.Attachments, showImages);
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

        // Show image notification at the end if not displaying images
        if (!showImages)
        {
            CheckAndNotifyImageOption(issue);
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
        FormatAttachmentDetails(attachment, false);
    }

    public void FormatAttachmentDetails(Attachment attachment, bool showImages)
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

        // 画像ファイルの場合はSixelでインライン表示 (showImagesがtrueの場合のみ)
        if (IsImageType(attachment.ContentType))
        {
            if (showImages)
            {
                AnsiConsole.WriteLine();
                AnsiConsole.MarkupLine("[bold]Preview:[/]");

                if (_apiClient != null)
                {
                    var httpClient = (_apiClient as RedmineApiClient)?.GetHttpClient();
                    var apiKey = (_apiClient as RedmineApiClient)?.GetApiKey();

                    if (httpClient != null)
                    {
                        SixelImageRenderer.RenderActualImage(
                            attachment.ContentUrl,
                            httpClient,
                            apiKey,
                            attachment.Filename,
                            200 // 最大幅を200ピクセルに設定
                        );
                    }
                }
                AnsiConsole.WriteLine();
            }
            else
            {
                // Show notification at the end
            }
        }

        // Show image notification at the end if not displaying images
        if (!showImages && IsImageType(attachment.ContentType))
        {
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("[dim]💡 This is an image attachment. Use --image option to display preview (requires DEC Sixel graphics compatible terminal).[/]");
        }
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


    private void CheckAndNotifyImageOption(Issue issue)
    {
        if (_apiClient == null || issue.Attachments == null || issue.Attachments.Count == 0)
        {
            return;
        }

        // Check for inline images in description
        var imageReferences = _imageDetector.DetectImageReferences(issue.Description);
        if (imageReferences.Count > 0)
        {
            var imageAttachments = _imageDetector.FindMatchingAttachments(issue.Attachments, imageReferences);
            if (imageAttachments.Count > 0)
            {
                AnsiConsole.MarkupLine("[dim]💡 This issue contains inline images. Use --image option to display them (requires DEC Sixel graphics compatible terminal).[/]");
                return;
            }
        }

        // Check for inline images in journal notes
        if (issue.Journals != null)
        {
            foreach (var journal in issue.Journals)
            {
                if (!string.IsNullOrWhiteSpace(journal.Notes))
                {
                    var journalImageRefs = _imageDetector.DetectImageReferences(journal.Notes);
                    if (journalImageRefs.Count > 0)
                    {
                        var journalImageAttachments = _imageDetector.FindMatchingAttachments(issue.Attachments, journalImageRefs);
                        if (journalImageAttachments.Count > 0)
                        {
                            AnsiConsole.MarkupLine("[dim]💡 This issue contains inline images in comments. Use --image option to display them (requires DEC Sixel graphics compatible terminal).[/]");
                            return;
                        }
                    }
                }
            }
        }
    }


    private static bool IsImageType(string contentType)
    {
        return contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase);
    }
}
