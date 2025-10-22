using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

using FluentAssertions;

using RedmineCLI.Utils;

using Xunit;

namespace RedmineCLI.Tests.Utils;

[Collection("AnsiConsole")]
public class SixelImageRendererTests
{
    private static readonly byte[] SampleBmpImage = Convert.FromBase64String(
        "Qk1GAAAAAAAAADYAAAAoAAAAAgAAAAIAAAABABgAAAAAABAAAAAAAAAAAAAAAAAAAAAAAAAAAAD/AP8AAAD/AAD///8AAA==");

    [Fact]
    public void RenderActualImage_ShouldReturnFalse_WhenResponseNotSuccessful()
    {
        var handler = new StubMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.NotFound));
        using var client = new HttpClient(handler);

        var originalOut = Console.Out;
        using var writer = new StringWriter();
        Console.SetOut(writer);
        try
        {
            var result = SixelImageRenderer.RenderActualImage("https://example.com/missing.png", client, null, "missing.png");
            result.Should().BeFalse();
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    [Fact]
    public void RenderActualImage_ShouldReturnFalse_WhenImageDecodeFails()
    {
        var handler = new StubMessageHandler(_ =>
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(new byte[] { 0, 1, 2, 3 })
            };
            return response;
        });

        using var client = new HttpClient(handler);

        var originalOut = Console.Out;
        using var writer = new StringWriter();
        Console.SetOut(writer);
        try
        {
            var result = SixelImageRenderer.RenderActualImage("https://example.com/invalid.png", client, null, "invalid.png");
            result.Should().BeFalse();
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    [Fact]
    public void RenderActualImage_ShouldHandleHttpExceptions()
    {
        var handler = new StubMessageHandler(_ => throw new HttpRequestException("boom"));
        using var client = new HttpClient(handler);

        var originalOut = Console.Out;
        using var writer = new StringWriter();
        Console.SetOut(writer);
        try
        {
            var result = SixelImageRenderer.RenderActualImage("https://example.com/boom.png", client, null, "boom.png");
            result.Should().BeFalse();
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    [Fact]
    public void RenderSixelImage_ShouldWriteControlSequences()
    {
        var method = typeof(SixelImageRenderer).GetMethod("RenderSixelImage", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        method.Should().NotBeNull();

        var pixels = new byte[]
        {
            255, 0, 0,
            0, 255, 0,
            0, 0, 255,
            255, 255, 255
        };

        var originalOut = Console.Out;
        using var writer = new StringWriter();
        Console.SetOut(writer);
        try
        {
            method!.Invoke(null, new object[] { pixels, 2, 2 });
        }
        finally
        {
            Console.SetOut(originalOut);
        }

        var output = writer.ToString();
        output.Should().Contain("\x1bP");
        output.Should().Contain("\x1b\\");
    }

    [Fact]
    public void ResizeImage_ShouldDownscalePixels()
    {
        var method = typeof(SixelImageRenderer).GetMethod("ResizeImage", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        method.Should().NotBeNull();

        var source = new byte[
            3 * 4
        ]
        {
            255, 0, 0,
            0, 255, 0,
            0, 0, 255,
            255, 255, 255
        };

        var result = method!.Invoke(null, new object[] { source, 2, 2, 1, 1 });

        result.Should().NotBeNull();
        var tuple = ((byte[] pixelData, int width, int height))result!;
        tuple.width.Should().Be(1);
        tuple.height.Should().Be(1);
        tuple.pixelData.Should().HaveCount(3);
    }

    private sealed class StubMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _handler;

        public StubMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> handler)
        {
            _handler = handler;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(_handler(request));
        }
    }
}
