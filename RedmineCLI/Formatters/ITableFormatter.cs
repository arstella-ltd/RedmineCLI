using RedmineCLI.Models;

namespace RedmineCLI.Formatters;

public interface ITableFormatter
{
    void FormatIssues(List<Issue> issues);
    void FormatIssueDetails(Issue issue);
    void SetTimeFormat(TimeFormat format);
    Task FormatAttachmentsAsync(List<Attachment> attachments);
    Task FormatAttachmentDetailsAsync(Attachment attachment);
}
