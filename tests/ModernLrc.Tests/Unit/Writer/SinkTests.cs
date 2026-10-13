using System.Buffers;
using System.Text;
using ModernLrc;
using ModernLrc.Model;

namespace ModernLrc.Tests.Unit.Writer;

/// <summary>Every sink renders the same text as <see cref="LrcWriter.Write(LrcDocument, LrcWriteOptions?)"/>;
/// byte sinks encode it with <see cref="LrcWriteOptions.Encoding"/>.</summary>
public sealed class SinkTests
{
    private static readonly LrcDocument Doc = new LrcDocumentBuilder()
        .WithTitle("Démo")
        .AddLine("00:01.00", "first line")
        .AddLine("00:02.00", "second", LrcVoice.Female)
        .AddEnhancedLine(
            LrcTimestamp.FromMilliseconds(3_000),
            [(LrcTimestamp.FromMilliseconds(3_000), "hello "), (LrcTimestamp.FromMilliseconds(3_500), "wörld")])
        .Build();

    [Fact]
    public void CharBufferWriter_MatchesString()
    {
        var bw = new ArrayBufferWriter<char>();
        LrcWriter.Write(Doc, bw);
        new string(bw.WrittenSpan).ShouldBe(LrcWriter.Write(Doc));
    }

    [Fact]
    public void ByteBufferWriter_DefaultsToUtf8WithoutBom()
    {
        var bw = new ArrayBufferWriter<byte>();
        LrcWriter.Write(Doc, bw);
        bw.WrittenSpan.ToArray().ShouldBe(Encoding.UTF8.GetBytes(LrcWriter.Write(Doc)));
    }

    [Fact]
    public void ByteBufferWriter_UsesConfiguredEncodingAndBom()
    {
        var options = new LrcWriteOptions { EmitByteOrderMark = true, Encoding = Encoding.BigEndianUnicode };
        var expected = Encoding.BigEndianUnicode.GetPreamble()
            .Concat(Encoding.BigEndianUnicode.GetBytes(LrcWriter.Write(Doc, options)))
            .ToArray();

        var bw = new ArrayBufferWriter<byte>();
        LrcWriter.Write(Doc, bw, options);

        bw.WrittenSpan.ToArray().ShouldBe(expected);
    }

    [Fact]
    public void Stream_EmitByteOrderMark_PrependsUtf8Bom()
    {
        using var ms = new MemoryStream();
        LrcWriter.Write(Doc, ms, new LrcWriteOptions { EmitByteOrderMark = true });
        ms.ToArray()[..3].ShouldBe(new byte[] { 0xEF, 0xBB, 0xBF });
    }

    [Fact]
    public void Stream_Unwritable_Throws()
    {
        using var ms = new MemoryStream(new byte[10], writable: false);
        Should.Throw<ArgumentException>(() => LrcWriter.Write(Doc, ms));
    }
}
