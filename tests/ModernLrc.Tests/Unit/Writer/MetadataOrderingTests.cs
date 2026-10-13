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

    [Fact]
    public void Alphabetical_RawTagsEmittedInKeyOrder()
    {
        var doc = new LrcDocumentBuilder()
            .WithRawTag("zzz", "last")
            .WithRawTag("aaa", "first")
            .WithRawTag("mmm", "middle")
            .Build();
        var options = new LrcWriteOptions { MetadataOrdering = LrcMetadataOrdering.Alphabetical };
        LrcWriter.Write(doc, options).ShouldBe("[aaa:first]\n[mmm:middle]\n[zzz:last]\n");
    }
}
