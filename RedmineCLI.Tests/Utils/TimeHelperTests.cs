using System;

using FluentAssertions;

using RedmineCLI.Models;
using RedmineCLI.Utils;

using Xunit;

namespace RedmineCLI.Tests.Utils;

public class TimeHelperTests
{
    private readonly ITimeHelper _timeHelper;
    private readonly DateTime _testUtcTime;

    public TimeHelperTests()
    {
        _timeHelper = new TimeHelper();
        _testUtcTime = new DateTime(2025, 01, 22, 15, 30, 0, DateTimeKind.Utc);
    }

    [Fact]
    public void GetRelativeTime_Should_ReturnMinutesAgo_When_LessThanHour()
    {
        // Arrange
        var utcNow = DateTime.UtcNow;
        var fiveMinutesAgo = utcNow.AddMinutes(-5);

        // Act
        var result = _timeHelper.GetRelativeTime(fiveMinutesAgo);

        // Assert
        result.Should().Be("5 minutes ago");
    }

    [Fact]
    public void GetRelativeTime_Should_ReturnMinuteAgo_When_OneMinute()
    {
        // Arrange
        var utcNow = DateTime.UtcNow;
        var oneMinuteAgo = utcNow.AddMinutes(-1);

        // Act
        var result = _timeHelper.GetRelativeTime(oneMinuteAgo);

        // Assert
        result.Should().Be("1 minute ago");
    }

    [Fact]
    public void GetRelativeTime_Should_ReturnHoursAgo_When_LessThanDay()
    {
        // Arrange
        var utcNow = DateTime.UtcNow;
        var twoHoursAgo = utcNow.AddHours(-2);

        // Act
        var result = _timeHelper.GetRelativeTime(twoHoursAgo);

        // Assert
        result.Should().Be("2 hours ago");
    }

    [Fact]
    public void GetRelativeTime_Should_ReturnHourAgo_When_OneHour()
    {
        // Arrange
        var utcNow = DateTime.UtcNow;
        var oneHourAgo = utcNow.AddHours(-1);

        // Act
        var result = _timeHelper.GetRelativeTime(oneHourAgo);

        // Assert
        result.Should().Be("1 hour ago");
    }

    [Fact]
    public void GetRelativeTime_Should_ReturnDaysAgo_When_LessThanMonth()
    {
        // Arrange
        var utcNow = DateTime.UtcNow;
        var threeDaysAgo = utcNow.AddDays(-3);

        // Act
        var result = _timeHelper.GetRelativeTime(threeDaysAgo);

        // Assert
        result.Should().Be("3 days ago");
    }

    [Fact]
    public void GetRelativeTime_Should_ReturnDayAgo_When_OneDay()
    {
        // Arrange
        var utcNow = DateTime.UtcNow;
        var oneDayAgo = utcNow.AddDays(-1);

        // Act
        var result = _timeHelper.GetRelativeTime(oneDayAgo);

        // Assert
        result.Should().Be("1 day ago");
    }

    [Fact]
    public void GetRelativeTime_Should_ReturnMonthDay_When_CurrentYear()
    {
        // Arrange
        var utcNow = DateTime.UtcNow;
        var twoMonthsAgo = utcNow.AddMonths(-2);

        // Act
        var result = _timeHelper.GetRelativeTime(twoMonthsAgo);

        // Assert
        result.Should().Match($"{twoMonthsAgo:MMM dd}");
    }

    [Fact]
    public void GetRelativeTime_Should_ReturnFullDate_When_PreviousYear()
    {
        // Arrange
        var utcNow = DateTime.UtcNow;
        var lastYear = utcNow.AddYears(-1);

        // Act
        var result = _timeHelper.GetRelativeTime(lastYear);

        // Assert
        result.Should().Match($"{lastYear:MMM dd, yyyy}");
    }

    [Fact]
    public void GetRelativeTime_Should_ReturnJustNow_When_LessThanMinute()
    {
        // Arrange
        var utcNow = DateTime.UtcNow;
        var thirtySecondsAgo = utcNow.AddSeconds(-30);

        // Act
        var result = _timeHelper.GetRelativeTime(thirtySecondsAgo);

        // Assert
        result.Should().Be("just now");
    }

    [Fact]
    public void ConvertToLocalTime_Should_ConvertFromUtc_When_ValidDateTime()
    {
        // Arrange
        var utcTime = new DateTime(2025, 01, 22, 15, 30, 0, DateTimeKind.Utc);

        // Act
        var localTime = _timeHelper.ConvertToLocalTime(utcTime);

        // Assert
        localTime.Kind.Should().Be(DateTimeKind.Local);
        localTime.Should().Be(utcTime.ToLocalTime());
    }

    [Fact]
    public void GetLocalTime_Should_ReturnFormattedLocalTime_When_ValidDateTime()
    {
        // Arrange
        var utcTime = new DateTime(2025, 01, 22, 15, 30, 0, DateTimeKind.Utc);
        var expectedLocalTime = utcTime.ToLocalTime();

        // Act
        var result = _timeHelper.GetLocalTime(utcTime);

        // Assert
        result.Should().Be(expectedLocalTime.ToString("yyyy-MM-dd HH:mm"));
    }

    [Fact]
    public void GetLocalTime_Should_UseCustomFormat_When_FormatProvided()
    {
        // Arrange
        var utcTime = new DateTime(2025, 01, 22, 15, 30, 0, DateTimeKind.Utc);
        var expectedLocalTime = utcTime.ToLocalTime();
        var customFormat = "dd/MM/yyyy HH:mm:ss";

        // Act
        var result = _timeHelper.GetLocalTime(utcTime, customFormat);

        // Assert
        result.Should().Be(expectedLocalTime.ToString(customFormat));
    }

    [Fact]
    public void GetUtcTime_Should_ReturnFormattedUtcTime_When_ValidDateTime()
    {
        // Arrange
        var utcTime = new DateTime(2025, 01, 22, 15, 30, 0, DateTimeKind.Utc);

        // Act
        var result = _timeHelper.GetUtcTime(utcTime);

        // Assert
        result.Should().Be("2025-01-22 15:30:00");
    }

    [Fact]
    public void GetUtcTime_Should_UseCustomFormat_When_FormatProvided()
    {
        // Arrange
        var utcTime = new DateTime(2025, 01, 22, 15, 30, 0, DateTimeKind.Utc);
        var customFormat = "yyyy-MM-ddTHH:mm:ssZ";

        // Act
        var result = _timeHelper.GetUtcTime(utcTime, customFormat);

        // Assert
        result.Should().Be(utcTime.ToString(customFormat));
    }

    [Fact]
    public void FormatTime_Should_UseConfiguredFormat_When_SettingExists()
    {
        // Arrange
        var utcNow = DateTime.UtcNow;
        var twoHoursAgo = utcNow.AddHours(-2);

        // Act
        var relativeResult = _timeHelper.FormatTime(twoHoursAgo, TimeFormat.Relative);
        var absoluteResult = _timeHelper.FormatTime(twoHoursAgo, TimeFormat.Absolute);
        var utcResult = _timeHelper.FormatTime(twoHoursAgo, TimeFormat.Utc);

        // Assert
        relativeResult.Should().Be("2 hours ago");
        absoluteResult.Should().Be(twoHoursAgo.ToLocalTime().ToString("yyyy-MM-dd HH:mm"));
        utcResult.Should().EndWith(" UTC");
    }
}
