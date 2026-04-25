using ModernLrc;
using ModernLrc.Model;

namespace ModernLrc.Tests.Unit.Parser;

public sealed class MultiTimestampParseTests
{
    [Fact]
    public void TwoTimestamps_BuildsOneLineWithBoth()
    {
        var result = LrcParser.Parse("[00:01.00][00:05.00]repeat me");
        result.Document.Lines.Count.ShouldBe(1);
        var line = (LrcPlainLine)result.Document.Lines[0];
        line.Timestamps.Count.ShouldBe(2);
        line.Timestamps[0].TotalMilliseconds.ShouldBe(1_000);
        line.Timestamps[1].TotalMilliseconds.ShouldBe(5_000);
        line.Text.ShouldBe("repeat me");
    }

    [Fact]
    public void ThreeTimestamps_AllPreserved()
    {
        var result = LrcParser.Parse("[00:01.00][00:05.00][01:30.00]chorus");
        var line = (LrcPlainLine)result.Document.Lines[0];
        line.Timestamps.Count.ShouldBe(3);
    }
}
