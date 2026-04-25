using ModernLrc;

namespace ModernLrc.Tests.Unit.Writer;

public sealed class MetadataOrderingTests
{
    [Fact]
    public void Canonical_OrdersStronglyTypedKeysSpecOrder()
    {
        var doc = new LrcDocumentBuilder()
            .WithVersion("1.0")
            .WithTool("Demo")
            .WithCreatedBy("Bob")
            .WithLyricist("Alice")
            .WithAuthor("Carol")
            .WithAlbum("Album")
            .WithArtist("Artist")
            .WithTitle("Title")
            .WithLength(TimeSpan.FromSeconds(180))
            .AddLine("00:01.00", "x")
            .Build();
        var output = LrcWriter.Write(doc, new LrcWriteOptions { MetadataOrdering = LrcMetadataOrdering.Canonical });
        // ti, ar, al, au, lr, length, by, offset, re, ve
        output.ShouldStartWith("[ti:Title]\n[ar:Artist]\n[al:Album]\n[au:Carol]\n[lr:Alice]\n[length:03:00]\n[by:Bob]\n[re:Demo]\n[ve:1.0]\n");
    }

    [Fact]
    public void Alphabetical_OrdersStronglyTypedKeysAToZ()
    {
        var doc = new LrcDocumentBuilder()
            .WithTitle("T").WithArtist("A").WithTool("X")
            .AddLine("00:01.00", "x")
            .Build();
        var output = LrcWriter.Write(doc, new LrcWriteOptions { MetadataOrdering = LrcMetadataOrdering.Alphabetical });
        var arIdx = output.IndexOf("[ar:", StringComparison.Ordinal);
        var tiIdx = output.IndexOf("[ti:", StringComparison.Ordinal);
        var reIdx = output.IndexOf("[re:", StringComparison.Ordinal);
        arIdx.ShouldBeLessThan(tiIdx, "Artist should precede Title");
        tiIdx.ShouldBeLessThan(reIdx, "Title should precede Tool (alphabetical by C# property name)");
    }

    [Fact]
    public void NoMetadata_NoBlankLineBeforeLyrics()
    {
        var doc = new LrcDocumentBuilder()
            .AddLine("00:01.00", "x")
            .Build();
        var output = LrcWriter.Write(doc);
        output.ShouldBe("[00:01.00]x\n");
    }

    [Fact]
    public void MetadataAndLyrics_BlankLineSeparator()
    {
        var doc = new LrcDocumentBuilder()
            .WithTitle("T")
            .AddLine("00:01.00", "x")
            .Build();
        var output = LrcWriter.Write(doc);
        output.ShouldBe("[ti:T]\n\n[00:01.00]x\n");
    }

    [Fact]
    public void OffsetZero_NotEmitted()
    {
        var doc = new LrcDocumentBuilder()
            .WithTitle("T")
            .AddLine("00:01.00", "x")
            .Build();
        LrcWriter.Write(doc).ShouldNotContain("[offset:");
    }

    [Fact]
    public void OffsetNegative_EmittedWithSign()
    {
        var doc = new LrcDocumentBuilder()
            .WithOffset(TimeSpan.FromMilliseconds(-150))
            .AddLine("00:01.00", "x")
            .Build();
        LrcWriter.Write(doc).ShouldContain("[offset:-150]");
    }

    [Fact]
    public void OffsetPositive_EmittedWithPlusSign()
    {
        var doc = new LrcDocumentBuilder()
            .WithOffset(TimeSpan.FromMilliseconds(250))
            .AddLine("00:01.00", "x")
            .Build();
        LrcWriter.Write(doc).ShouldContain("[offset:+250]");
    }
}
