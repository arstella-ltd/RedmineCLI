using RedmineCLI.Models;

namespace RedmineCLI.Formatters;

public interface IJsonFormatter
{
    void FormatIssues(List<Issue> issues);
    void FormatIssueDetails(Issue issue, bool showImages = false);
    void FormatAttachments(List<Attachment> attachments);
    void FormatAttachmentDetails(Attachment attachment);
    void FormatObject<T>(T obj);
    void FormatUsers(List<User> users, bool showAllDetails = false);
    void FormatProjects(List<Project> projects);
    void FormatIssueStatuses(List<IssueStatus> statuses);
    void FormatPriorities(List<Priority> priorities);
}
