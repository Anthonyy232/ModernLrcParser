using System.Text;
using ModernLrc;
using ModernLrc.Diagnostics;

namespace ModernLrc.Tests.Unit.Parser;

public sealed class EncodingPipelineTests
{
    [Fact]
    public void Utf8WithBom_Decoded()
    {
        var bytes = new byte[] { 0xEF, 0xBB, 0xBF }
            .Concat(Encoding.UTF8.GetBytes("[00:01.00]hello"))
            .ToArray();
        var result = LrcParser.Parse(bytes);
        result.Document.Lines.Count.ShouldBe(1);
    }

    [Fact]
    public void Utf16Le_DecodedFromBom()
    {
        var bytes = new byte[] { 0xFF, 0xFE }
            .Concat(Encoding.Unicode.GetBytes("[00:01.00]hi"))
            .ToArray();
        var result = LrcParser.Parse(bytes);
        result.Document.Lines.Count.ShouldBe(1);
    }

    [Fact]
    public void NoBom_Utf8_Detected()
    {
        var bytes = Encoding.UTF8.GetBytes("[00:01.00]hello");
        var result = LrcParser.Parse(bytes);
        result.Document.Lines.Count.ShouldBe(1);
    }

    [Fact]
    public void NoBom_NoFallback_InvalidUtf8_Throws()
    {
        // Truncated 3-byte UTF-8 sequence — not valid UTF-8
        var bytes = new byte[] { 0xE2, 0x82 };
        var options = new LrcParseOptions { FallbackEncoding = null };
        Should.Throw<LrcParseException>(() => LrcParser.Parse(bytes, options));
    }

    [Fact]
    public void Parse_FromMemoryStream_ProducesExpectedDocument()
    {
        using var ms = new MemoryStream(Encoding.UTF8.GetBytes("[00:01.00]stream"));
        var result = LrcParser.Parse(ms);
        result.Document.Lines.Count.ShouldBe(1);
    }

    [Fact]
    public void EncodingFallback_EmittedAtErrorSeverity()
    {
        // Latin-1 encoded "café" — not valid UTF-8 (`\xE9` byte); needs fallback.
        var bytes = System.Text.Encoding.Latin1.GetBytes("[00:01.00]café");
        var options = new LrcParseOptions { FallbackEncoding = System.Text.Encoding.Latin1 };
        var result = LrcParser.Parse(bytes, options);
        result.Diagnostics.ShouldContain(d =>
            d.Code == LrcDiagnosticIds.EncodingFallback
            && d.Severity == LrcDiagnosticSeverity.Error);
    }

    [Fact]
    public void EncodingFailure_FromParseFile_AnnotatesFilePath()
    {
        var tmp = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"modernlrc-encoding-{System.Guid.NewGuid():N}.lrc");
        try
        {
            // Write an invalid-UTF-8 byte sequence (0xE2 0x82 truncated; no BOM).
            System.IO.File.WriteAllBytes(tmp, new byte[] { 0xE2, 0x82 });
            var options = new LrcParseOptions { FallbackEncoding = null };
            var ex = Should.Throw<LrcParseException>(() =>
                LrcParser.ParseFile(tmp, options));
            ex.FilePath.ShouldBe(tmp);
        }
        finally
        {
            if (System.IO.File.Exists(tmp)) System.IO.File.Delete(tmp);
        }
    }
}
