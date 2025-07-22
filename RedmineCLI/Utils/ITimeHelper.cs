using RedmineCLI.Models;

namespace RedmineCLI.Utils;

public interface ITimeHelper
{
    string GetRelativeTime(DateTime utcTime);
    string GetLocalTime(DateTime utcTime, string format = "yyyy-MM-dd HH:mm");
    string GetUtcTime(DateTime utcTime, string format = "yyyy-MM-dd HH:mm:ss");
    DateTime ConvertToLocalTime(DateTime utcTime);
    string FormatTime(DateTime dateTime, TimeFormat format);
}