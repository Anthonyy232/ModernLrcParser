using ModernLrc;
using ModernLrc.Model;

namespace ModernLrc.Tests.Unit.Model;

public sealed class LrcDocumentExtensionsTests
{
    private static LrcDocument BuildDoc(TimeSpan offset, params (long ms, string text)[] lines)
    {
        var arr = new LrcLine[lines.Length];
        for (int i = 0; i < lines.Length; i++)
        {
            arr[i] = new LrcPlainLine
            {
                Timestamp = LrcTimestamp.FromMilliseconds(lines[i].ms),
                Text = lines[i].text,
            };
        }
        return new LrcDocument
        {
            Metadata = new LrcMetadata { Offset = offset },
            Lines = System.Collections.Immutable.ImmutableArray.Create(arr),
        };
    }

    [Fact]
    public void GetEffectiveTime_NoOffset_ReturnsSame()
    {
        var doc = LrcDocument.Empty;
        var t = LrcTimestamp.FromMilliseconds(1_000);
        doc.GetEffectiveTime(t).ShouldBe(TimeSpan.FromMilliseconds(1_000));
    }

    [Fact]
    public void GetEffectiveTime_NegativeOffset_CanProduceNegativeTimeSpan()
    {
        var doc = BuildDoc(TimeSpan.FromMilliseconds(-500));
        var t = LrcTimestamp.FromMilliseconds(200);
        doc.GetEffectiveTime(t).TotalMilliseconds.ShouldBe(-300);
    }

    [Fact]
    public void FindLineAt_EmptyDoc_ReturnsNull()
    {
        LrcDocument.Empty.FindLineAt(TimeSpan.FromSeconds(10)).ShouldBeNull();
    }

    [Fact]
    public void FindLineAt_BeforeFirst_ReturnsNull()
    {
        var doc = BuildDoc(TimeSpan.Zero, (1_000, "a"), (2_000, "b"));
        doc.FindLineAt(TimeSpan.FromMilliseconds(500)).ShouldBeNull();
    }

    [Fact]
    public void FindLineAt_AtFirstTimestamp_ReturnsFirstLine()
    {
        var doc = BuildDoc(TimeSpan.Zero, (1_000, "a"), (2_000, "b"));
        var line = doc.FindLineAt(TimeSpan.FromMilliseconds(1_000));
        ((LrcPlainLine)line!).Text.ShouldBe("a");
    }

    [Fact]
    public void FindLineAt_BetweenLines_ReturnsPriorLine()
    {
        var doc = BuildDoc(TimeSpan.Zero, (1_000, "a"), (2_000, "b"), (3_000, "c"));
        var line = doc.FindLineAt(TimeSpan.FromMilliseconds(2_500));
        ((LrcPlainLine)line!).Text.ShouldBe("b");
    }

    [Fact]
    public void FindLineAt_PastEnd_ReturnsLastLine()
    {
        var doc = BuildDoc(TimeSpan.Zero, (1_000, "a"), (2_000, "b"));
        var line = doc.FindLineAt(TimeSpan.FromMilliseconds(99_999));
        ((LrcPlainLine)line!).Text.ShouldBe("b");
    }

    [Fact]
    public void FindLineAt_HonoursOffset()
    {
        // offset = -500 means line at 1_000 effectively starts at 500.
        var doc = BuildDoc(TimeSpan.FromMilliseconds(-500), (1_000, "a"), (2_000, "b"));
        var line = doc.FindLineAt(TimeSpan.FromMilliseconds(500));
        ((LrcPlainLine)line!).Text.ShouldBe("a");
    }

    [Fact]
    public void LinesInRange_HalfOpen_ExcludesEnd()
    {
        var doc = BuildDoc(TimeSpan.Zero, (1_000, "a"), (2_000, "b"), (3_000, "c"));
        var range = doc.LinesInRange(
            TimeSpan.FromMilliseconds(1_000),
            TimeSpan.FromMilliseconds(3_000)).ToList();
        range.Count.ShouldBe(2);
        ((LrcPlainLine)range[0]).Text.ShouldBe("a");
        ((LrcPlainLine)range[1]).Text.ShouldBe("b");
    }

    [Fact]
    public void LinesInRange_EmptyDoc_ReturnsEmpty()
    {
        LrcDocument.Empty.LinesInRange(TimeSpan.Zero, TimeSpan.FromHours(1)).ShouldBeEmpty();
    }

    [Fact]
    public void LinesInRange_PreservesDocumentOrder_OnTies()
    {
        // Two lines with the same timestamp — original order must be preserved.
        var t = LrcTimestamp.FromMilliseconds(1_000);
        var doc = new LrcDocument
        {
            Lines =
            [
                new LrcPlainLine { Timestamp = t, Text = "first" },
                new LrcPlainLine { Timestamp = t, Text = "second" },
            ],
        };
        var range = doc.LinesInRange(TimeSpan.Zero, TimeSpan.FromSeconds(2)).ToList();
        ((LrcPlainLine)range[0]).Text.ShouldBe("first");
        ((LrcPlainLine)range[1]).Text.ShouldBe("second");
    }

    [Fact]
    public void GetText_PlainLine_ReturnsText()
    {
        LrcLine line = new LrcPlainLine
        {
            Timestamp = LrcTimestamp.Zero,
            Text = "hello world",
        };
        line.GetText().ShouldBe("hello world");
    }

    [Fact]
    public void GetText_EnhancedLine_ConcatsWords()
    {
        var t = LrcTimestamp.Zero;
        LrcLine line = new LrcEnhancedLine
        {
            Timestamp = t,
            Words = [new(t, "hello "), new(t, "world")],
        };
        line.GetText().ShouldBe("hello world");
    }

    [Fact]
    public void GetText_EnhancedLine_Empty_ReturnsEmpty()
    {
        LrcLine line = new LrcEnhancedLine
        {
            Timestamp = LrcTimestamp.Zero,
            Words = EquatableArray<LrcWord>.Empty,
        };
        line.GetText().ShouldBe(string.Empty);
    }
}
