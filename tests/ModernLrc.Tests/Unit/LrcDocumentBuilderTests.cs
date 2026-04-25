using System.Globalization;
using ModernLrc;
using ModernLrc.Model;

namespace ModernLrc.Tests.Unit;

public sealed class LrcDocumentBuilderTests
{
    // ---- Task 37: construction, metadata, and Build() ----

    [Fact]
    public void Build_FromEmpty_ProducesEmptyDocument()
    {
        var doc = new LrcDocumentBuilder().Build();
        doc.Lines.Count.ShouldBe(0);
        doc.Metadata.ShouldBe(LrcMetadata.Empty);
    }

    [Fact]
    public void WithTitle_FluentReturn()
    {
        var b = new LrcDocumentBuilder();
        b.WithTitle("Song").ShouldBeSameAs(b);
        b.Build().Metadata.Title.ShouldBe("Song");
    }

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
    public void WithRawTag_NullKey_Throws()
    {
        Should.Throw<ArgumentNullException>(() =>
            new LrcDocumentBuilder().WithRawTag(null!, "v"));
    }

    [Fact]
    public void WithRawTag_NullValue_Throws()
    {
        Should.Throw<ArgumentNullException>(() =>
            new LrcDocumentBuilder().WithRawTag("k", null!));
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
    public void Clear_DropsEverything()
    {
        var b = new LrcDocumentBuilder()
            .WithTitle("X")
            .AddLine("00:01.00", "hello");
        b.Clear();
        var doc = b.Build();
        doc.Metadata.ShouldBe(LrcMetadata.Empty);
        doc.Lines.Count.ShouldBe(0);
    }

    // ---- Task 38: AddLine and AddEnhancedLine variants ----

    [Fact]
    public void AddLine_StringTimestamp_BuildsPlainLine()
    {
        var doc = new LrcDocumentBuilder()
            .AddLine("00:12.34", "hello")
            .Build();
        doc.Lines.Count.ShouldBe(1);
        var line = (LrcPlainLine)doc.Lines[0];
        line.Text.ShouldBe("hello");
        line.Timestamps[0].ShouldBe(LrcTimestamp.Parse("00:12.34", CultureInfo.InvariantCulture));
    }

    [Fact]
    public void AddLine_LrcTimestampOverload_Works()
    {
        var t = LrcTimestamp.FromMilliseconds(1_500);
        var doc = new LrcDocumentBuilder()
            .AddLine(t, "x")
            .Build();
        ((LrcPlainLine)doc.Lines[0]).Timestamps[0].ShouldBe(t);
    }

    [Fact]
    public void AddLine_TimeSpanOverload_Works()
    {
        var ts = TimeSpan.FromMilliseconds(2_500);
        var doc = new LrcDocumentBuilder()
            .AddLine(ts, "x")
            .Build();
        ((LrcPlainLine)doc.Lines[0]).Timestamps[0].ShouldBe(LrcTimestamp.FromTimeSpan(ts));
    }

    [Fact]
    public void AddLine_TimeSpanNegative_Throws()
    {
        Should.Throw<ArgumentOutOfRangeException>(() =>
            new LrcDocumentBuilder().AddLine(TimeSpan.FromMilliseconds(-1), "x"));
    }

    [Fact]
    public void AddLine_MultiTimestamp_PreservesAll()
    {
        var t1 = LrcTimestamp.FromMilliseconds(1_000);
        var t2 = LrcTimestamp.FromMilliseconds(2_000);
        var doc = new LrcDocumentBuilder()
            .AddLine([t1, t2], "x")
            .Build();
        ((LrcPlainLine)doc.Lines[0]).Timestamps.Count.ShouldBe(2);
    }

    [Fact]
    public void AddLine_EmptyTimestampsForMulti_Throws()
    {
        Should.Throw<ArgumentException>(() =>
            new LrcDocumentBuilder().AddLine(ReadOnlySpan<LrcTimestamp>.Empty, "x"));
    }

    [Fact]
    public void AddLine_TextNull_Throws()
    {
        Should.Throw<ArgumentNullException>(() =>
            new LrcDocumentBuilder().AddLine("00:01.00", null!));
    }

    [Fact]
    public void AddLine_TimestampStringNull_Throws()
    {
        Should.Throw<ArgumentNullException>(() =>
            new LrcDocumentBuilder().AddLine((string)null!, "x"));
    }

    [Fact]
    public void AddLine_TimestampStringInvalid_Throws()
    {
        Should.Throw<FormatException>(() =>
            new LrcDocumentBuilder().AddLine("garbage", "x"));
    }

    [Fact]
    public void AddLine_VoiceCarriedToLine()
    {
        var doc = new LrcDocumentBuilder()
            .AddLine("00:01.00", "hi", LrcVoice.Female)
            .Build();
        var line = (LrcPlainLine)doc.Lines[0];
        line.EffectiveVoice.ShouldBe(LrcVoice.Female);
    }

    [Fact]
    public void AddEnhancedLine_SpanOfWords_Works()
    {
        var t0 = LrcTimestamp.FromMilliseconds(0);
        var t1 = LrcTimestamp.FromMilliseconds(500);
        ReadOnlySpan<LrcWord> words = [new(t0, "hello "), new(t1, "world")];
        var doc = new LrcDocumentBuilder()
            .AddEnhancedLine(t0, words)
            .Build();
        var line = (LrcEnhancedLine)doc.Lines[0];
        line.Words.Count.ShouldBe(2);
        line.Words[1].Text.ShouldBe("world");
    }

    [Fact]
    public void AddEnhancedLine_FromTuples_Works()
    {
        var t0 = LrcTimestamp.FromMilliseconds(0);
        var t1 = LrcTimestamp.FromMilliseconds(500);
        var doc = new LrcDocumentBuilder()
            .AddEnhancedLine(t0, [(t0, "hello "), (t1, "world")])
            .Build();
        var line = (LrcEnhancedLine)doc.Lines[0];
        line.Words.Count.ShouldBe(2);
    }

    [Fact]
    public void AddEnhancedLine_MultiTimestamp_Works()
    {
        var t1 = LrcTimestamp.FromMilliseconds(1_000);
        var t2 = LrcTimestamp.FromMilliseconds(2_000);
        ReadOnlySpan<LrcTimestamp> stamps = [t1, t2];
        ReadOnlySpan<LrcWord> words = [new(t1, "x")];
        var doc = new LrcDocumentBuilder()
            .AddEnhancedLine(stamps, words)
            .Build();
        var line = (LrcEnhancedLine)doc.Lines[0];
        line.Timestamps.Count.ShouldBe(2);
    }

    [Fact]
    public void AddEnhancedLine_TuplesNull_Throws()
    {
        var t = LrcTimestamp.Zero;
        Should.Throw<ArgumentNullException>(() =>
            new LrcDocumentBuilder().AddEnhancedLine(t, (IEnumerable<(LrcTimestamp, string)>)null!));
    }

    [Fact]
    public void AddEnhancedLine_VoiceCarried()
    {
        var t = LrcTimestamp.Zero;
        var doc = new LrcDocumentBuilder()
            .AddEnhancedLine(t, [(t, "x")], LrcVoice.Male)
            .Build();
        var line = (LrcEnhancedLine)doc.Lines[0];
        line.EffectiveVoice.ShouldBe(LrcVoice.Male);
    }

    [Fact]
    public void AddLine_LrcLineDirect_HoldsReference()
    {
        var t = LrcTimestamp.FromMilliseconds(500);
        var line = new LrcPlainLine { Timestamps = [t], Text = "x" };
        var doc = new LrcDocumentBuilder().AddLine(line).Build();
        doc.Lines[0].ShouldBeSameAs(line);
    }

    [Fact]
    public void AddLine_LrcLineWithEmptyTimestamps_Throws()
    {
        var bad = new LrcPlainLine { Timestamps = EquatableArray<LrcTimestamp>.Empty, Text = "x" };
        Should.Throw<ArgumentException>(() => new LrcDocumentBuilder().AddLine(bad));
    }

    [Fact]
    public void AddLines_AppendsAll()
    {
        var t = LrcTimestamp.FromMilliseconds(1_000);
        LrcLine[] lines =
        [
            new LrcPlainLine { Timestamps = [t], Text = "a" },
            new LrcPlainLine { Timestamps = [t + TimeSpan.FromMilliseconds(1)], Text = "b" },
        ];
        var doc = new LrcDocumentBuilder().AddLines(lines).Build();
        doc.Lines.Count.ShouldBe(2);
    }

    // ---- Task 39: manipulation, ShiftAll, and Build sort ----

    [Fact]
    public void LineCount_ReflectsInsertions()
    {
        var b = new LrcDocumentBuilder();
        b.LineCount.ShouldBe(0);
        b.AddLine("00:01.00", "a");
        b.LineCount.ShouldBe(1);
        b.AddLine("00:02.00", "b");
        b.LineCount.ShouldBe(2);
    }

    [Fact]
    public void GetLineAt_ReturnsInsertionOrder()
    {
        var b = new LrcDocumentBuilder()
            .AddLine("00:02.00", "second")
            .AddLine("00:01.00", "first");
        ((LrcPlainLine)b.GetLineAt(0)).Text.ShouldBe("second");
        ((LrcPlainLine)b.GetLineAt(1)).Text.ShouldBe("first");
    }

    [Fact]
    public void GetLineAt_OutOfRange_Throws()
    {
        var b = new LrcDocumentBuilder().AddLine("00:01.00", "a");
        Should.Throw<ArgumentOutOfRangeException>(() => b.GetLineAt(5));
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
        var replacement = new LrcPlainLine { Timestamps = [t], Text = "replaced" };
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
        ((LrcPlainLine)doc.Lines[0]).Timestamps[0].TotalMilliseconds.ShouldBe(1_500);
        ((LrcPlainLine)doc.Lines[1]).Timestamps[0].TotalMilliseconds.ShouldBe(2_500);
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
        line.Timestamps[0].TotalMilliseconds.ShouldBe(1_500);
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
        // Original timestamps must be intact.
        var doc = b.Build();
        ((LrcPlainLine)doc.Lines[0]).Timestamps[0].TotalMilliseconds.ShouldBe(500);
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
            Lines = [new LrcPlainLine { Timestamps = [t], Text = "y" }],
        };
        var b = new LrcDocumentBuilder(source);
        b.LineCount.ShouldBe(1);
        var doc = b.Build();
        doc.Metadata.Title.ShouldBe("X");
        ((LrcPlainLine)doc.Lines[0]).Text.ShouldBe("y");
    }

    [Fact]
    public void CtorFromExistingDocument_NullSource_Throws()
    {
        Should.Throw<ArgumentNullException>(() => new LrcDocumentBuilder(null!));
    }

    // ---- Boundary tests added during code review ----

    [Fact]
    public void RemoveLineAt_OutOfRange_Throws()
    {
        var b = new LrcDocumentBuilder().AddLine("00:01.00", "x");
        Should.Throw<ArgumentOutOfRangeException>(() => b.RemoveLineAt(5));
        Should.Throw<ArgumentOutOfRangeException>(() => b.RemoveLineAt(-1));
    }

    [Fact]
    public void RemoveLinesWhere_NullPredicate_Throws()
    {
        Should.Throw<ArgumentNullException>(() =>
            new LrcDocumentBuilder().RemoveLinesWhere(null!));
    }

    [Fact]
    public void ReplaceLine_OutOfRange_Throws()
    {
        var b = new LrcDocumentBuilder().AddLine("00:01.00", "x");
        var t = LrcTimestamp.FromMilliseconds(1_000);
        var replacement = new LrcPlainLine { Timestamps = [t], Text = "y" };
        Should.Throw<ArgumentOutOfRangeException>(() => b.ReplaceLine(5, replacement));
        Should.Throw<ArgumentOutOfRangeException>(() => b.ReplaceLine(-1, replacement));
    }

    [Fact]
    public void ReplaceLine_NullReplacement_Throws()
    {
        var b = new LrcDocumentBuilder().AddLine("00:01.00", "x");
        Should.Throw<ArgumentNullException>(() => b.ReplaceLine(0, null!));
    }

    [Fact]
    public void ShiftAll_TimeSpanMinValue_NormalizesOverflowToArgumentOutOfRange()
    {
        var b = new LrcDocumentBuilder().AddLine("00:01.00", "x");
        Should.Throw<ArgumentOutOfRangeException>(() => b.ShiftAll(TimeSpan.MinValue));
        // Builder unchanged: "00:01.00" is 1 second (1000 ms).
        ((LrcPlainLine)b.GetLineAt(0)).Timestamps[0].TotalMilliseconds.ShouldBe(1_000);
    }
}
