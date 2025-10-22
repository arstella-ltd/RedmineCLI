using System;

using FluentAssertions;

using RedmineCLI.Utils;

using Xunit;

namespace RedmineCLI.Tests.Utils;

public class SixelEncoderTests
{
    [Fact]
    public void Encode_ShouldIncludeSixelHeadersAndFooter()
    {
        var encoder = new SixelEncoder();
        var pixelData = new byte[] { 255, 0, 0 };

        var result = encoder.Encode(pixelData, 1, 1, 3);

        result.Should().StartWith("\x1bP8;0;0q");
        result.Should().Contain("#0;2;100;0;0");
        result.Should().EndWith("\x1b\\");
    }

    [Fact]
    public void Encode_ShouldThrow_WhenPixelDataInsufficient()
    {
        var encoder = new SixelEncoder();

        Action act = () => encoder.Encode(Array.Empty<byte>(), 1, 1, 3);

        act.Should().Throw<ArgumentException>()
            .WithMessage("Insufficient pixel data");
    }

    [Fact]
    public void Encode_ShouldApplyRunLengthEncodingForRepeatedPixels()
    {
        var encoder = new SixelEncoder();

        var pixelData = new byte[]
        {
            0, 0, 0,
            0, 0, 0,
            0, 0, 0,
            0, 0, 0,
            0, 0, 0,
            0, 0, 0
        };

        var result = encoder.Encode(pixelData, 1, 6, 3);

        result.Should().Contain("#0~");
    }
}
