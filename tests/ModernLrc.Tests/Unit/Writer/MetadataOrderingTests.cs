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
    public void Alphabetical_SortsTypedAndRawTagsTogetherByKey()
    {
        var doc = new LrcDocumentBuilder()
            .WithTitle("T").WithRawTag("mm", "1").WithTool("X").WithArtist("A").WithRawTag("mm", "2")
            .Build();
        var options = new LrcWriteOptions { MetadataOrdering = LrcMetadataOrdering.Alphabetical };
        LrcWriter.Write(doc, options).ShouldBe("[ar:A]\n[mm:1]\n[mm:2]\n[re:X]\n[ti:T]\n");
    }

    [Fact]
    public void Alphabetical_EditedTypedTag_IsSortedWithParsedOnes()
    {
        var parsed = LrcParser.Parse("[ve:1]\n[ar:Y]").Document;
        var edited = parsed with { Metadata = parsed.Metadata with { Title = "X" } };
        var options = new LrcWriteOptions { MetadataOrdering = LrcMetadataOrdering.Alphabetical };
        LrcWriter.Write(edited, options).ShouldBe("[ar:Y]\n[ti:X]\n[ve:1]\n");
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
