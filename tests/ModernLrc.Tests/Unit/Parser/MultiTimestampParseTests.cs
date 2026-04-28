using ModernLrc;
using ModernLrc.Model;

namespace ModernLrc.Tests.Unit.Parser;

public sealed class MultiTimestampParseTests
{
    [Fact]
    public void TwoTimestamps_FansOutIntoTwoLines_SharingText()
    {
        var result = LrcParser.Parse("[00:01.00][00:05.00]repeat me");
        result.Document.Lines.Count.ShouldBe(2);

        var a = (LrcPlainLine)result.Document.Lines[0];
        var b = (LrcPlainLine)result.Document.Lines[1];

        a.Timestamp.TotalMilliseconds.ShouldBe(1_000);
        b.Timestamp.TotalMilliseconds.ShouldBe(5_000);
        a.Text.ShouldBe("repeat me");
        b.Text.ShouldBe("repeat me");

        // The fan-out should share the same string instance — multi-timestamp groups
        // pay for one Text allocation, not N.
        ReferenceEquals(a.Text, b.Text).ShouldBeTrue();
    }

    [Fact]
    public void ThreeTimestamps_FansOutIntoThreeLines()
    {
        var result = LrcParser.Parse("[00:01.00][00:05.00][01:30.00]chorus");
        result.Document.Lines.Count.ShouldBe(3);
        foreach (var line in result.Document.Lines)
            ((LrcPlainLine)line).Text.ShouldBe("chorus");
    }
}
