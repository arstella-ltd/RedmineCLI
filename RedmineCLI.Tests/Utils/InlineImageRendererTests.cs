using System.Collections.Generic;

using FluentAssertions;

using RedmineCLI.Models;
using RedmineCLI.Tests.TestInfrastructure;
using RedmineCLI.Utils;

using Xunit;

namespace RedmineCLI.Tests.Utils;

[Collection("AnsiConsole")]
public class InlineImageRendererTests
{
    private readonly AnsiConsoleTestFixture _fixture;

    public InlineImageRendererTests(AnsiConsoleTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public void RenderTextWithInlineImages_ShouldHighlightImageReferences()
    {
        var renderer = new InlineImageRenderer(null);
        var attachments = new List<Attachment>
        {
            new()
            {
                Filename = "image.png",
                ContentType = "image/png",
                ContentUrl = "https://example.com/image.png"
            }
        };

        var output = _fixture.ExecuteWithTestConsole(console =>
        {
            renderer.RenderTextWithInlineImages("Intro ![alt](image.png) Outro", attachments, false);
            return console.Output.ToString();
        });

        output.Should().Contain("Intro ![alt](image.png) Outro");
    }

    [Fact]
    public void RenderTextWithInlineImages_ShouldPrintPlainText_WhenAttachmentsMissing()
    {
        var renderer = new InlineImageRenderer(null);

        var output = _fixture.ExecuteWithTestConsole(console =>
        {
            renderer.RenderTextWithInlineImages("Plain text only", null, true);
            return console.Output.ToString();
        });

        output.Should().Contain("Plain text only");
    }
}
