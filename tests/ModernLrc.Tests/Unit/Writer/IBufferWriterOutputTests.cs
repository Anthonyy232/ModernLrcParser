using System.Buffers;
using ModernLrc;
using ModernLrc.Model;

namespace ModernLrc.Tests.Unit.Writer;

/// <summary>The <see cref="IBufferWriter{T}"/> overloads of <see cref="LrcWriter.Write(LrcDocument, LrcWriteOptions?)"/>
/// must produce the same logical output as the string overload — for both char and UTF-8 byte
/// sinks, and across documents large enough to span multiple buffer leases.</summary>
public sealed class IBufferWriterOutputTests
{
    [Fact]
    public void Write_ToCharBufferWriter_OutputMatchesStringOverload()
    {
        var doc = BuildSampleDoc();
        var stringOutput = LrcWriter.Write(doc);

        var bw = new ArrayBufferWriter<char>();
        LrcWriter.Write(doc, bw);
        var bwOutput = new string(bw.WrittenSpan);

        bwOutput.ShouldBe(stringOutput);
    }

    [Fact]
    public void Write_ToUtf8BufferWriter_OutputMatchesStringOverload_DecodedAsUtf8()
    {
        var doc = BuildSampleDoc();
        var stringOutput = LrcWriter.Write(doc);

        var bw = new ArrayBufferWriter<byte>();
        LrcWriter.Write(doc, bw);
        var bwOutput = System.Text.Encoding.UTF8.GetString(bw.WrittenSpan);

        bwOutput.ShouldBe(stringOutput);
    }

    [Fact]
    public void Write_ToCharBufferWriter_LongDocument_OutputMatchesStringOverload()
    {
        // 200 lines is well past the default initial buffer; this verifies that crossing
        // multiple GetSpan/Advance cycles still produces the same logical content.
        var builder = new LrcDocumentBuilder();
        for (int i = 0; i < 200; i++)
            builder.AddLine(LrcTimestamp.FromMilliseconds(i * 100), $"line {i}");
        var doc = builder.Build();
        var stringOutput = LrcWriter.Write(doc);

        var bw = new ArrayBufferWriter<char>();
        LrcWriter.Write(doc, bw);
        var bwOutput = new string(bw.WrittenSpan);

        bwOutput.ShouldBe(stringOutput);
    }

    private static LrcDocument BuildSampleDoc() =>
        new LrcDocumentBuilder()
            .WithTitle("Demo").WithArtist("Tester")
            .WithOffset(TimeSpan.FromMilliseconds(-150))
            .AddLine("00:01.00", "first line")
            .AddLine("00:02.00", "second", LrcVoice.Female)
            .AddEnhancedLine(
                LrcTimestamp.FromMilliseconds(3_000),
                [(LrcTimestamp.FromMilliseconds(3_000), "hello "), (LrcTimestamp.FromMilliseconds(3_500), "world")])
            .Build();
}
