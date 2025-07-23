using RedmineCLI.Models;

namespace RedmineCLI.Formatters;

public interface ITableFormatter
{
    void FormatIssues(List<Issue> issues);
    void FormatIssueDetails(Issue issue);
    void SetTimeFormat(TimeFormat format);
    void FormatAttachments(List<Attachment> attachments);
    void FormatAttachmentDetails(Attachment attachment);
}
