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
            return "less than a minute ago";

        // Less than an hour
        if (diff.TotalMinutes < 60)
        {
            var minutes = (int)diff.TotalMinutes;
            return FormatDuration(minutes, "minute");
        }

        // Less than a day
        if (diff.TotalHours < 24)
        {
            var hours = (int)diff.TotalHours;
            return FormatDuration(hours, "hour");
        }

        // Less than 30 days
        if (diff.TotalDays < 30)
        {
            var days = (int)diff.TotalDays;
            return FormatDuration(days, "day");
        }

        // Less than a year (365 days)
        if (diff.TotalDays < 365)
        {
            var months = (int)(diff.TotalDays / 30);
            return FormatDuration(months, "month");
        }

        // Years
        var years = (int)(diff.TotalDays / 365);
        return FormatDuration(years, "year");
    }

    private static string FormatDuration(int amount, string unit)
    {
        var pluralizedUnit = amount == 1 ? unit : $"{unit}s";
        return $"about {amount} {pluralizedUnit} ago";
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
