using System.Globalization;

using RedmineCLI.Models;

namespace RedmineCLI.Utils;

public class TimeHelper : ITimeHelper
{
    public string GetRelativeTime(DateTime utcTime)
    {
        var now = DateTime.UtcNow;
        var diff = now - utcTime;

        // Less than a minute
        if (diff.TotalSeconds < 60)
            return "just now";

        // Less than an hour
        if (diff.TotalMinutes < 60)
        {
            var minutes = (int)diff.TotalMinutes;
            return minutes == 1 ? "1 minute ago" : $"{minutes} minutes ago";
        }

        // Less than a day
        if (diff.TotalHours < 24)
        {
            var hours = (int)diff.TotalHours;
            return hours == 1 ? "1 hour ago" : $"{hours} hours ago";
        }

        // Less than 30 days
        if (diff.TotalDays < 30)
        {
            var days = (int)diff.TotalDays;
            return days == 1 ? "1 day ago" : $"{days} days ago";
        }

        // Same year
        if (utcTime.Year == now.Year)
        {
            return utcTime.ToString("MMM dd", CultureInfo.InvariantCulture);
        }

        // Previous years
        return utcTime.ToString("MMM dd, yyyy", CultureInfo.InvariantCulture);
    }

    public string GetLocalTime(DateTime utcTime, string format = "yyyy-MM-dd HH:mm")
    {
        var localTime = ConvertToLocalTime(utcTime);
        return localTime.ToString(format, CultureInfo.InvariantCulture);
    }

    public string GetUtcTime(DateTime utcTime, string format = "yyyy-MM-dd HH:mm:ss")
    {
        return utcTime.ToString(format, CultureInfo.InvariantCulture);
    }

    public DateTime ConvertToLocalTime(DateTime utcTime)
    {
        if (utcTime.Kind == DateTimeKind.Unspecified)
        {
            // Assume unspecified times are UTC
            utcTime = DateTime.SpecifyKind(utcTime, DateTimeKind.Utc);
        }
        return utcTime.ToLocalTime();
    }

    public string FormatTime(DateTime dateTime, TimeFormat format)
    {
        return format switch
        {
            TimeFormat.Relative => GetRelativeTime(dateTime),
            TimeFormat.Absolute => GetLocalTime(dateTime),
            TimeFormat.Utc => GetUtcTime(dateTime) + " UTC",
            _ => GetRelativeTime(dateTime)
        };
    }
}
