using RedmineCLI.Models;

namespace RedmineCLI.Formatters;

public interface IJsonFormatter
{
    void FormatIssues(List<Issue> issues);
}