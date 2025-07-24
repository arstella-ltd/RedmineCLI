using FluentAssertions;

using RedmineCLI.Models;
using RedmineCLI.Utils;

using Xunit;

namespace RedmineCLI.Tests.Utils;

public class ImageReferenceDetectorTests
{
    private readonly ImageReferenceDetector _detector;

    public ImageReferenceDetectorTests()
    {
        _detector = new ImageReferenceDetector();
    }

    [Theory]
    [InlineData("![alt text](image.png)", "image.png")]
    [InlineData("![](screenshot.jpg)", "screenshot.jpg")]
    [InlineData("![画像の説明](日本語ファイル名.png)", "日本語ファイル名.png")]
    [InlineData("{{thumbnail(diagram.png)}}", "diagram.png")]
    [InlineData("{{image(photo.jpeg)}}", "photo.jpeg")]
    public void DetectImageReferences_Should_ReturnFilename_When_ValidImageReference(string description, string expectedFilename)
    {
        // Act
        var references = _detector.DetectImageReferences(description);

        // Assert
        references.Should().ContainSingle();
        references.First().Should().Be(expectedFilename);
    }

    [Theory]
    [InlineData("通常のテキストです")]
    [InlineData("[リンク](http://example.com)")]
    [InlineData("{{code}}")]
    public void DetectImageReferences_Should_ReturnEmpty_When_NoImageReference(string description)
    {
        // Act
        var references = _detector.DetectImageReferences(description);

        // Assert
        references.Should().BeEmpty();
    }

    [Fact]
    public void DetectImageReferences_Should_ReturnMultipleFilenames_When_MultipleReferences()
    {
        // Arrange
        var description = @"
## 概要
![スクリーンショット1](screen1.png)
ここに説明があります。

{{thumbnail(diagram.png)}}

最後に別の画像 ![別の画像](screen2.jpg) があります。
";

        // Act
        var references = _detector.DetectImageReferences(description);

        // Assert
        references.Should().HaveCount(3);
        references.Should().Contain("screen1.png");
        references.Should().Contain("diagram.png");
        references.Should().Contain("screen2.jpg");
    }

    [Fact]
    public void FindMatchingAttachments_Should_ReturnMatchingAttachments_When_FilenamesMatch()
    {
        // Arrange
        var attachments = new List<Attachment>
        {
            new Attachment { Id = 1, Filename = "screen1.png", ContentType = "image/png" },
            new Attachment { Id = 2, Filename = "document.pdf", ContentType = "application/pdf" },
            new Attachment { Id = 3, Filename = "diagram.png", ContentType = "image/png" }
        };
        var references = new List<string> { "screen1.png", "diagram.png" };

        // Act
        var matching = _detector.FindMatchingAttachments(attachments, references);

        // Assert
        matching.Should().HaveCount(2);
        matching.Should().Contain(a => a.Id == 1);
        matching.Should().Contain(a => a.Id == 3);
    }

    [Fact]
    public void FindMatchingAttachments_Should_OnlyReturnImages_When_ContentTypeIsImage()
    {
        // Arrange
        var attachments = new List<Attachment>
        {
            new Attachment { Id = 1, Filename = "test.png", ContentType = "image/png" },
            new Attachment { Id = 2, Filename = "test.png", ContentType = "text/plain" }, // 同じファイル名だが画像ではない
            new Attachment { Id = 3, Filename = "photo.jpg", ContentType = "image/jpeg" }
        };
        var references = new List<string> { "test.png", "photo.jpg" };

        // Act
        var matching = _detector.FindMatchingAttachments(attachments, references);

        // Assert
        matching.Should().HaveCount(2);
        matching.Should().Contain(a => a.Id == 1);
        matching.Should().Contain(a => a.Id == 3);
        matching.Should().NotContain(a => a.Id == 2); // text/plainは除外
    }

    [Theory]
    [InlineData("image/png", true)]
    [InlineData("image/jpeg", true)]
    [InlineData("image/jpg", true)]
    [InlineData("image/gif", true)]
    [InlineData("image/bmp", true)]
    [InlineData("image/webp", true)]
    [InlineData("image/svg+xml", true)]
    [InlineData("text/plain", false)]
    [InlineData("application/pdf", false)]
    [InlineData("", false)]
    public void IsImageContentType_Should_ReturnCorrectValue_When_CheckingContentType(string contentType, bool expected)
    {
        // Act
        var result = _detector.IsImageContentType(contentType);

        // Assert
        result.Should().Be(expected);
    }
}
