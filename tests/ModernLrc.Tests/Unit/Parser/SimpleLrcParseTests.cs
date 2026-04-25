using ModernLrc;
using ModernLrc.Model;

namespace ModernLrc.Tests.Unit.Parser;

public sealed class SimpleLrcParseTests
{
    [Fact]
    public void SingleLine_BuildsOnePlainLine()
    {
        var result = LrcParser.Parse("[00:01.00]hello");
        result.HasErrors.ShouldBeFalse();
        result.Document.Lines.Count.ShouldBe(1);
        var line = (LrcPlainLine)result.Document.Lines[0];
        line.Text.ShouldBe("hello");
        line.Timestamps[0].TotalMilliseconds.ShouldBe(1_000);
    }

    [Fact]
    public void TwoLines_PreservedInOrder()
    {
        var result = LrcParser.Parse("[00:01.00]first\n[00:02.00]second");
        result.Document.Lines.Count.ShouldBe(2);
        ((LrcPlainLine)result.Document.Lines[0]).Text.ShouldBe("first");
        ((LrcPlainLine)result.Document.Lines[1]).Text.ShouldBe("second");
    }
}
