using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace RedmineCLI.Utils;

/// <summary>
/// Intercepts error messages to fix formatting issues with System.CommandLine
/// </summary>
public class ErrorMessageInterceptor : TextWriter
{
    private readonly TextWriter _originalWriter;
    private readonly StringBuilder _buffer = new();
    private static readonly Regex DoublePeriodPattern = new(@"[。。]\.(?=\s|$)", RegexOptions.Compiled);

    public ErrorMessageInterceptor(TextWriter originalWriter)
    {
        _originalWriter = originalWriter;
    }

    public override Encoding Encoding => _originalWriter.Encoding;

    public override void Write(char value)
    {
        _buffer.Append(value);
        if (value == '\n')
        {
            FlushBuffer();
        }
    }

    public override void Write(string? value)
    {
        if (value == null) return;

        _buffer.Append(value);
        if (value.Contains('\n'))
        {
            FlushBuffer();
        }
    }

    public override void WriteLine(string? value)
    {
        if (value != null)
        {
            _buffer.Append(value);
        }
        _buffer.AppendLine();
        FlushBuffer();
    }

    public override void WriteLine()
    {
        _buffer.AppendLine();
        FlushBuffer();
    }

    private void FlushBuffer()
    {
        if (_buffer.Length == 0) return;

        var content = _buffer.ToString();
        _buffer.Clear();

        // Fix double period issue: remove period after Japanese period (。)
        content = DoublePeriodPattern.Replace(content, "");

        _originalWriter.Write(content);
    }

    public override void Flush()
    {
        FlushBuffer();
        _originalWriter.Flush();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            FlushBuffer();
            // Restore original error writer
            Console.SetError(_originalWriter);
        }
        base.Dispose(disposing);
    }
}
