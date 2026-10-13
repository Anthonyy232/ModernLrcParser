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

        ReferenceEquals(a.Text, b.Text).ShouldBeTrue();
    }
}
