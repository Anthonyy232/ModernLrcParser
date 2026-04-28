using CsCheck;
using ModernLrc;
using ModernLrc.Model;
using ModernLrc.Tests.Generators;

namespace ModernLrc.Tests.Properties;

public sealed class RoundTripProperties
{
    [Fact]
    public void PlainLine_AuthorWriteParse_PreservesText()
    {
        // Property: for any single-line LRC document built from constrained text,
        // Parse(Write(doc)) must yield a document with the same text and timestamp.
        Gen.Select(LrcGen.Timestamp, LrcGen.SimpleText)
            .Sample((ts, text) =>
            {
                var doc = new LrcDocumentBuilder()
                    .AddLine(ts, text)
                    .Build();
                var lrc = LrcWriter.Write(doc);
                var parsed = LrcParser.Parse(lrc);
                if (parsed.HasErrors) return false;
                if (parsed.Document.Lines.Count != 1) return false;
                var line = (LrcPlainLine)parsed.Document.Lines[0];
                return line.Text == text && line.Timestamp == ts;
            }, iter: 1000);
    }

    [Fact]
    public void Tolerant_ParseNeverThrows_OnArbitraryBmpInput()
    {
        // Property: any string of BMP code units (excluding lone surrogates) parses in
        // Tolerant mode without throwing. The full character range matters here — '[', ']',
        // '<', '>', '\r', '\n', and NUL each exercise distinct recovery paths in the scanner
        // that ASCII-printable input would never hit.
        Gen.Char[(char)0, (char)0xD7FF].Array[0, 200]
            .Select(chars => new string(chars))
            .Sample(input =>
            {
                Should.NotThrow(() => LrcParser.Parse(input));
                return true;
            }, iter: 1000);
    }
}
