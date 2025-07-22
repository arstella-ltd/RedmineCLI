namespace RedmineCLI.Models;

public enum TimeFormat
{
    Relative,    // "2 hours ago"
    Absolute,    // Local time "2025-01-22 09:30"
    Utc          // UTC time "2025-01-22 00:30 UTC"
}