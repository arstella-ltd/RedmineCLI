using RedmineCLI.Models;

namespace RedmineCLI.Formatters;

public interface IJsonFormatter
{
    void FormatIssues(List<Issue> issues);
    void FormatIssueDetails(Issue issue);
    void FormatAttachments(List<Attachment> attachments);
    void FormatAttachmentDetails(Attachment attachment);
    void FormatObject<T>(T obj);
}
