using ModernLrc;
using ModernLrc.Model;

namespace ModernLrc.Tests.Unit.Writer;

public sealed class EnhancedWriteTests
{
    [Fact]
    public void EnhancedLine_RendersWordsInOrder()
    {
        var t = LrcTimestamp.FromMilliseconds(1_000);
        var t2 = LrcTimestamp.FromMilliseconds(1_500);
        var doc = new LrcDocumentBuilder()
            .AddEnhancedLine(t, [(t, "hello "), (t2, "world")])
            .Build();
        LrcWriter.Write(doc).ShouldBe("[00:01.00]<00:01.00>hello <00:01.50>world\n");
    }

    [Fact]
    public void EnhancedLine_NoWords_RendersTimestampOnly()
    {
        var t = LrcTimestamp.Zero;
        var doc = new LrcDocumentBuilder()
            .AddEnhancedLine(t, ReadOnlySpan<LrcWord>.Empty)
            .Build();
        LrcWriter.Write(doc).ShouldBe("[00:00.00]\n");
    }
}
