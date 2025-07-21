namespace RedmineCLI.Models;

public class IssueFilter
{
    public string? AssignedToId { get; set; }
    public string? ProjectId { get; set; }
    public string? StatusId { get; set; }
    public int? Limit { get; set; }
    public int? Offset { get; set; }
}
