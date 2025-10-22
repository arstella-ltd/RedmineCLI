using System;

using FluentAssertions;

using RedmineCLI.Utils;

using Xunit;

namespace RedmineCLI.Tests.Utils;

public class StbImageSharpImageDecoderTests
{
    private static readonly byte[] SampleBmpImage = Convert.FromBase64String(
        "Qk1GAAAAAAAAADYAAAAoAAAAAgAAAAIAAAABABgAAAAAABAAAAAAAAAAAAAAAAAAAAAAAAAAAAD/AP8AAAD/AAD///8AAA==");

    [Fact]
    public void DecodeImage_ShouldReturnData_WhenImageValid()
    {
        var result = StbImageSharpImageDecoder.DecodeImage(SampleBmpImage);

        result.Should().NotBeNull();
        var (pixels, width, height) = result!.Value;
        width.Should().Be(2);
        height.Should().Be(2);
        pixels.Should().HaveCount(width * height * 3);
    }

    [Fact]
    public void DecodeImage_ShouldReturnNull_WhenImageInvalid()
    {
        var result = StbImageSharpImageDecoder.DecodeImage(new byte[] { 0, 1, 2, 3 });
        result.Should().BeNull();
    }
}
