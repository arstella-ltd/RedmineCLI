using RedmineCLI.Models;

namespace RedmineCLI.Formatters;

public interface IJsonFormatter
{
    void FormatIssues(List<Issue> issues);
    void FormatIssueDetails(Issue issue);
    Task FormatAttachmentsAsync(List<Attachment> attachments);
    Task FormatAttachmentDetailsAsync(Attachment attachment);
}
