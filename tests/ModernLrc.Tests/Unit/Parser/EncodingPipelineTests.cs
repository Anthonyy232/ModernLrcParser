using System.Text;
using ModernLrc;
using ModernLrc.Diagnostics;

namespace ModernLrc.Tests.Unit.Parser;

public sealed class EncodingPipelineTests
{
    [Theory]
    [InlineData("utf-8")]
    [InlineData("utf-16")]
    [InlineData("utf-16BE")]
    public void Bom_SelectsEncoding(string encodingName)
    {
        var encoding = Encoding.GetEncoding(encodingName);
        var bytes = encoding.GetPreamble().Concat(encoding.GetBytes("[00:01.00]héllo")).ToArray();
        var result = LrcParser.Parse(bytes);
        ((ModernLrc.Model.LrcPlainLine)result.Document.Lines[0]).Text.ShouldBe("héllo");
    }

    [Fact]
    public void NoBom_NoFallback_InvalidUtf8_Throws()
    {
        var bytes = new byte[] { 0xE2, 0x82 };
        var options = new LrcParseOptions { FallbackEncoding = null };
        Should.Throw<LrcParseException>(() => LrcParser.Parse(bytes, options));
    }

    [Fact]
    public void DefaultFallback_DecodesLatin1Losslessly()
    {
        var bytes = System.Text.Encoding.Latin1.GetBytes("[00:01.00]café");
        var result = LrcParser.Parse(bytes);
        ((ModernLrc.Model.LrcPlainLine)result.Document.Lines[0]).Text.ShouldBe("café");
        result.Diagnostics.ShouldContain(d =>
            d.Code == LrcDiagnosticIds.EncodingFallback && d.Severity == LrcDiagnosticSeverity.Error);
    }

    [Fact]
    public void EncodingFailure_FromParseFile_AnnotatesFilePath()
    {
        var tmp = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"modernlrc-encoding-{System.Guid.NewGuid():N}.lrc");
        try
        {
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
