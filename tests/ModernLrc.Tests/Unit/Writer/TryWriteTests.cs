using ModernLrc;

namespace ModernLrc.Tests.Unit.Writer;

public sealed class TryWriteTests
{
    [Fact]
    public void TryWrite_Char_SufficientBuffer_Succeeds()
    {
        var doc = new LrcDocumentBuilder().AddLine("00:01.00", "x").Build();
        Span<char> dest = stackalloc char[64];
        LrcWriter.TryWrite(doc, dest, out int written).ShouldBeTrue();
        new string(dest[..written]).ShouldBe("[00:01.00]x\n");
    }

    [Fact]
    public void TryWrite_Char_TooSmall_Returns_False()
    {
        var doc = new LrcDocumentBuilder().AddLine("00:01.00", "x").Build();
        Span<char> dest = stackalloc char[3];
        LrcWriter.TryWrite(doc, dest, out int written).ShouldBeFalse();
        written.ShouldBe(0);
    }

    [Fact]
    public void TryWrite_Byte_RoundTripsViaUTF8()
    {
        var doc = new LrcDocumentBuilder().AddLine("00:01.00", "x").Build();
        Span<byte> dest = stackalloc byte[64];
        LrcWriter.TryWrite(doc, dest, out int written).ShouldBeTrue();
        System.Text.Encoding.UTF8.GetString(dest[..written]).ShouldBe("[00:01.00]x\n");
    }
}
