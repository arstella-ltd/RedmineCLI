using RedmineCLI.Models;

namespace RedmineCLI.Formatters;

public interface ITableFormatter
{
    void FormatIssues(List<Issue> issues);
    void FormatIssueDetails(Issue issue);
}