using RedmineCLI.Models;

namespace RedmineCLI.Formatters;

public interface ITableFormatter
{
    void FormatIssues(List<Issue> issues);
    void FormatIssueDetails(Issue issue);
    void FormatIssueDetails(Issue issue, bool showImages);
    void SetTimeFormat(TimeFormat format);
    void FormatAttachments(List<Attachment> attachments);
    void FormatAttachmentDetails(Attachment attachment);
    void FormatAttachmentDetails(Attachment attachment, bool showImages);
    void FormatUsers(List<User> users);
    void FormatProjects(List<Project> projects);
    void FormatIssueStatuses(List<IssueStatus> statuses);
    void FormatPriorities(List<Priority> priorities);
}
