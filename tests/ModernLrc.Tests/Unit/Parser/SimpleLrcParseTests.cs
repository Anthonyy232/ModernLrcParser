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
        line.Timestamp.TotalMilliseconds.ShouldBe(1_000);
    }
}
