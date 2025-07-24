using System;

using FluentAssertions;

using RedmineCLI.Utils;

using Xunit;

namespace RedmineCLI.Tests.Utils;

public class TerminalCapabilityDetectorTests
{
    [Fact]
    public void SupportsSixel_Should_AlwaysReturnFalse()
    {
        // SupportsSixelは非推奨となり、常にfalseを返す
        var result = TerminalCapabilityDetector.SupportsSixel();

        // 常にfalseを返すことを確認
        result.Should().BeFalse();
    }

    [Fact]
    public void SupportsSixel_Should_ReturnConsistentValue_When_CalledMultipleTimes()
    {
        // Arrange & Act
        var firstCall = TerminalCapabilityDetector.SupportsSixel();
        var secondCall = TerminalCapabilityDetector.SupportsSixel();

        // Assert
        firstCall.Should().BeFalse();
        secondCall.Should().BeFalse();
    }

    [Fact]
    public void GetTerminalWidth_Should_ReturnValidValue()
    {
        // Act
        var width = TerminalCapabilityDetector.GetTerminalWidth();

        // Assert
        // CI環境やリダイレクト時は0またはデフォルト値を返す可能性がある
        width.Should().BeGreaterThanOrEqualTo(0);
        // デフォルト値の80以下であることを確認
        if (width > 0)
        {
            width.Should().BeGreaterThanOrEqualTo(10); // 最小幅
        }
    }

}
