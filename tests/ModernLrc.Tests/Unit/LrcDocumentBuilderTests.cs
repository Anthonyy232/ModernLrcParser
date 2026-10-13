using System.Globalization;
using ModernLrc;
using ModernLrc.Model;

namespace ModernLrc.Tests.Unit;

public sealed class LrcDocumentBuilderTests
{
    [Fact]
    public void Metadata_AllSetters_PreserveValues()
    {
        var doc = new LrcDocumentBuilder()
            .WithTitle("T")
            .WithArtist("A")
            .WithAlbum("AL")
            .WithAuthor("AU")
            .WithLyricist("L")
            .WithCreatedBy("B")
            .WithTool("TL")
            .WithVersion("V")
            .WithLength(TimeSpan.FromSeconds(180))
            .WithOffset(TimeSpan.FromMilliseconds(-150))
            .Build();
        var m = doc.Metadata;
        m.Title.ShouldBe("T");
        m.Artist.ShouldBe("A");
        m.Album.ShouldBe("AL");
        m.Author.ShouldBe("AU");
        m.Lyricist.ShouldBe("L");
        m.CreatedBy.ShouldBe("B");
        m.Tool.ShouldBe("TL");
        m.Version.ShouldBe("V");
        m.Length!.Value.TotalSeconds.ShouldBe(180);
        m.Offset.TotalMilliseconds.ShouldBe(-150);
    }

    [Fact]
    public void WithTitle_Null_ClearsValue()
    {
        var doc = new LrcDocumentBuilder()
            .WithTitle("X")
            .WithTitle(null)
            .Build();
        doc.Metadata.Title.ShouldBeNull();
    }

    [Fact]
    public void WithRawTag_AddsToCollection()
    {
        var doc = new LrcDocumentBuilder()
            .WithRawTag("custom", "value")
            .WithRawTag("custom2", "value2")
            .Build();
        doc.Metadata.RawTags.Count.ShouldBe(2);
        doc.Metadata.RawTags[0].Key.ShouldBe("custom");
        doc.Metadata.RawTags[1].Value.ShouldBe("value2");
    }

    [Fact]
    public void WithRawTag_WhitespaceKey_Throws()
    {
        // "[ :value]" would not parse back.
        Should.Throw<ArgumentException>(() => new LrcDocumentBuilder().WithRawTag("   ", "value"));
    }

    [Fact]
    public void AddEnhancedLine_DefaultWord_Throws()
    {
        // default(LrcWord) has a null Text.
        LrcWord[] words = [default];
        Should.Throw<ArgumentException>(() =>
            new LrcDocumentBuilder().AddEnhancedLine(LrcTimestamp.Zero, (ReadOnlySpan<LrcWord>)words));
    }

    [Fact]
    public void RemoveRawTag_DropsByKey()
    {
        var doc = new LrcDocumentBuilder()
            .WithRawTag("a", "1")
            .WithRawTag("b", "2")
            .WithRawTag("a", "3")
            .RemoveRawTag("a")
            .Build();
        doc.Metadata.RawTags.Count.ShouldBe(1);
        doc.Metadata.RawTags[0].Key.ShouldBe("b");
    }

    [Fact]
    public void ClearMetadata_DropsAllMetadata_ButKeepsLines()
    {
        var b = new LrcDocumentBuilder()
            .WithTitle("X")
            .WithRawTag("k", "v")
            .AddLine("00:01.00", "hello");
        b.ClearMetadata();
        var doc = b.Build();
        doc.Metadata.ShouldBe(LrcMetadata.Empty);
        doc.Lines.Count.ShouldBe(1);
    }

    [Fact]
    public void ClearLines_DropsLines_ButKeepsMetadata()
    {
        var b = new LrcDocumentBuilder()
            .WithTitle("X")
            .AddLine("00:01.00", "hello");
        b.ClearLines();
        var doc = b.Build();
        doc.Metadata.Title.ShouldBe("X");
        doc.Lines.Count.ShouldBe(0);
    }

    [Fact]
    public void AddLine_StringTimestamp_BuildsPlainLine()
    {
        var doc = new LrcDocumentBuilder()
            .AddLine("00:12.34", "hello")
            .Build();
        doc.Lines.Count.ShouldBe(1);
        var line = (LrcPlainLine)doc.Lines[0];
        line.Text.ShouldBe("hello");
        line.Timestamp.ShouldBe(LrcTimestamp.Parse("00:12.34", CultureInfo.InvariantCulture));
    }

    [Fact]
    public void AddLineGroup_FansOutIntoOneLinePerTimestamp()
    {
        var t1 = LrcTimestamp.FromMilliseconds(1_000);
        var t2 = LrcTimestamp.FromMilliseconds(2_000);
        var doc = new LrcDocumentBuilder()
            .AddLineGroup([t1, t2], "x")
            .Build();
        doc.Lines.Count.ShouldBe(2);
        ((LrcPlainLine)doc.Lines[0]).Timestamp.ShouldBe(t1);
        ((LrcPlainLine)doc.Lines[1]).Timestamp.ShouldBe(t2);
        ReferenceEquals(((LrcPlainLine)doc.Lines[0]).Text, ((LrcPlainLine)doc.Lines[1]).Text)
            .ShouldBeTrue();
    }

    [Fact]
    public void AddLineGroup_EmptyTimestamps_Throws()
    {
        Should.Throw<ArgumentException>(() =>
            new LrcDocumentBuilder().AddLineGroup(ReadOnlySpan<LrcTimestamp>.Empty, "x"));
    }

    [Fact]
    public void AddEnhancedLineGroup_FansOutIntoOneLinePerTimestamp()
    {
        var t1 = LrcTimestamp.FromMilliseconds(1_000);
        var t2 = LrcTimestamp.FromMilliseconds(2_000);
        ReadOnlySpan<LrcTimestamp> stamps = [t1, t2];
        ReadOnlySpan<LrcWord> words = [new(t1, "x")];
        var doc = new LrcDocumentBuilder()
            .AddEnhancedLineGroup(stamps, words)
            .Build();
        doc.Lines.Count.ShouldBe(2);
        ((LrcEnhancedLine)doc.Lines[0]).Timestamp.ShouldBe(t1);
        ((LrcEnhancedLine)doc.Lines[1]).Timestamp.ShouldBe(t2);
        ((LrcEnhancedLine)doc.Lines[0]).Words.Equals(((LrcEnhancedLine)doc.Lines[1]).Words).ShouldBeTrue();
    }

    [Fact]
    public void RemoveLineAt_RemovesAtIndex()
    {
        var doc = new LrcDocumentBuilder()
            .AddLine("00:01.00", "a")
            .AddLine("00:02.00", "b")
            .AddLine("00:03.00", "c")
            .RemoveLineAt(1)
            .Build();
        doc.Lines.Count.ShouldBe(2);
        ((LrcPlainLine)doc.Lines[0]).Text.ShouldBe("a");
        ((LrcPlainLine)doc.Lines[1]).Text.ShouldBe("c");
    }

    [Fact]
    public void RemoveLinesWhere_PredicateFilters()
    {
        var doc = new LrcDocumentBuilder()
            .AddLine("00:01.00", "keep")
            .AddLine("00:02.00", "drop")
            .AddLine("00:03.00", "keep")
            .RemoveLinesWhere(l => ((LrcPlainLine)l).Text == "drop")
            .Build();
        doc.Lines.Count.ShouldBe(2);
    }

    [Fact]
    public void ReplaceLine_SwapsAtIndex()
    {
        var t = LrcTimestamp.FromMilliseconds(2_000);
        var replacement = new LrcPlainLine { Timestamp = t, Text = "replaced" };
        var doc = new LrcDocumentBuilder()
            .AddLine("00:01.00", "a")
            .AddLine("00:02.00", "b")
            .ReplaceLine(1, replacement)
            .Build();
        ((LrcPlainLine)doc.Lines[1]).Text.ShouldBe("replaced");
    }

    [Fact]
    public void ShiftAll_PositiveDelta_AddsToEveryTimestamp()
    {
        var doc = new LrcDocumentBuilder()
            .AddLine("00:01.00", "a")
            .AddLine("00:02.00", "b")
            .ShiftAll(TimeSpan.FromMilliseconds(500))
            .Build();
        ((LrcPlainLine)doc.Lines[0]).Timestamp.TotalMilliseconds.ShouldBe(1_500);
        ((LrcPlainLine)doc.Lines[1]).Timestamp.TotalMilliseconds.ShouldBe(2_500);
    }

    [Fact]
    public void ShiftAll_AffectsEnhancedWords()
    {
        var t0 = LrcTimestamp.FromMilliseconds(1_000);
        var t1 = LrcTimestamp.FromMilliseconds(1_500);
        var doc = new LrcDocumentBuilder()
            .AddEnhancedLine(t0, [(t0, "a "), (t1, "b")])
            .ShiftAll(TimeSpan.FromMilliseconds(500))
            .Build();
        var line = (LrcEnhancedLine)doc.Lines[0];
        line.Timestamp.TotalMilliseconds.ShouldBe(1_500);
        line.Words[0].Timestamp.TotalMilliseconds.ShouldBe(1_500);
        line.Words[1].Timestamp.TotalMilliseconds.ShouldBe(2_000);
    }

    [Fact]
    public void ShiftAll_NegativeDeltaThatGoesBelowZero_ThrowsAndDoesNotMutate()
    {
        var b = new LrcDocumentBuilder()
            .AddLine("00:00.50", "a")
            .AddLine("00:02.00", "b");
        Should.Throw<ArgumentOutOfRangeException>(() =>
            b.ShiftAll(TimeSpan.FromMilliseconds(-1_000)));
        var doc = b.Build();
        ((LrcPlainLine)doc.Lines[0]).Timestamp.TotalMilliseconds.ShouldBe(500);
    }

    [Fact]
    public void Build_SortsLinesByFirstTimestamp()
    {
        var doc = new LrcDocumentBuilder()
            .AddLine("00:03.00", "c")
            .AddLine("00:01.00", "a")
            .AddLine("00:02.00", "b")
            .Build();
        ((LrcPlainLine)doc.Lines[0]).Text.ShouldBe("a");
        ((LrcPlainLine)doc.Lines[1]).Text.ShouldBe("b");
        ((LrcPlainLine)doc.Lines[2]).Text.ShouldBe("c");
    }

    [Fact]
    public void Build_StableSort_PreservesInsertionOrderOnTies()
    {
        var doc = new LrcDocumentBuilder()
            .AddLine("00:01.00", "first")
            .AddLine("00:01.00", "second")
            .AddLine("00:01.00", "third")
            .Build();
        ((LrcPlainLine)doc.Lines[0]).Text.ShouldBe("first");
        ((LrcPlainLine)doc.Lines[1]).Text.ShouldBe("second");
        ((LrcPlainLine)doc.Lines[2]).Text.ShouldBe("third");
    }

    [Fact]
    public void Build_IsIdempotent_DoesNotMutateBuilder()
    {
        var b = new LrcDocumentBuilder()
            .AddLine("00:02.00", "b")
            .AddLine("00:01.00", "a");
        var d1 = b.Build();
        var d2 = b.Build();
        d1.ShouldBe(d2);
        // Builder still in insertion order, not sorted.
        ((LrcPlainLine)b.GetLineAt(0)).Text.ShouldBe("b");
    }

    [Fact]
    public void CtorFromExistingDocument_SeedsState()
    {
        var t = LrcTimestamp.FromMilliseconds(1_000);
        var source = new LrcDocument
        {
            Metadata = new LrcMetadata { Title = "X" },
            Lines = [new LrcPlainLine { Timestamp = t, Text = "y" }],
        };
        var b = new LrcDocumentBuilder(source);
        b.LineCount.ShouldBe(1);
        var doc = b.Build();
        doc.Metadata.Title.ShouldBe("X");
        ((LrcPlainLine)doc.Lines[0]).Text.ShouldBe("y");
    }

    [Fact]
    public void ShiftAll_TimeSpanMinValue_NormalizesOverflowToArgumentOutOfRange()
    {
        var b = new LrcDocumentBuilder().AddLine("00:01.00", "x");
        Should.Throw<ArgumentOutOfRangeException>(() => b.ShiftAll(TimeSpan.MinValue));
        ((LrcPlainLine)b.GetLineAt(0)).Timestamp.TotalMilliseconds.ShouldBe(1_000);
    }
}
