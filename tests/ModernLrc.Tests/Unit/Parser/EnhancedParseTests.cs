using ModernLrc;
using ModernLrc.Model;

namespace ModernLrc.Tests.Unit.Parser;

public sealed class EnhancedParseTests
{
    [Fact]
    public void EnhancedLine_ParsesWordsWithTimestamps()
    {
        var result = LrcParser.Parse("[00:01.00]<00:01.00>hello <00:01.50>world");
        var line = (LrcEnhancedLine)result.Document.Lines[0];
        line.Words.Count.ShouldBe(2);
        line.Words[0].Text.ShouldBe("hello ");
        line.Words[0].Timestamp.TotalMilliseconds.ShouldBe(1_000);
        line.Words[1].Text.ShouldBe("world");
        line.Words[1].Timestamp.TotalMilliseconds.ShouldBe(1_500);
    }

    [Fact]
    public void EnhancedLine_ConcatenatedWordsReproduceLineText()
    {
        var result = LrcParser.Parse("[00:01.00]<00:01.00>foo <00:01.30>bar");
        var line = (LrcEnhancedLine)result.Document.Lines[0];
        var concat = string.Concat(line.Words.AsImmutableArray().Select(w => w.Text));
        concat.ShouldBe("foo bar");
    }

    [Fact]
    public void EnhancedLine_UnclosedAngle_EmitsError()
    {
        var result = LrcParser.Parse("[00:01.00]<00:01.00 incomplete");
        result.HasErrors.ShouldBeTrue();
        result.Diagnostics.ShouldContain(d => d.Code == "LRC0008");
    }
}
