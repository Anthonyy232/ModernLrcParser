using ModernLrc;
using ModernLrc.Model;

namespace ModernLrc.Tests.Unit.Writer;

public sealed class SimpleWriteTests
{
    [Fact]
    public void EmptyDocument_RendersEmpty()
    {
        var output = LrcWriter.Write(LrcDocument.Empty);
        output.ShouldBe(string.Empty);
    }

    [Fact]
    public void SinglePlainLine_RendersTimestampPlusText()
    {
        var doc = new LrcDocumentBuilder()
            .AddLine("00:01.00", "hello")
            .Build();
        var output = LrcWriter.Write(doc);
        output.ShouldBe("[00:01.00]hello\n");
    }

    [Fact]
    public void TwoLines_SeparatedByLineFeed()
    {
        var doc = new LrcDocumentBuilder()
            .AddLine("00:01.00", "first")
            .AddLine("00:02.00", "second")
            .Build();
        var output = LrcWriter.Write(doc);
        output.ShouldBe("[00:01.00]first\n[00:02.00]second\n");
    }

    [Fact]
    public void Crlf_LineEnding_AppliedConsistently()
    {
        var doc = new LrcDocumentBuilder()
            .AddLine("00:01.00", "x")
            .Build();
        var options = new LrcWriteOptions { LineEnding = LrcLineEnding.Crlf };
        var output = LrcWriter.Write(doc, options);
        output.ShouldBe("[00:01.00]x\r\n");
    }

    [Fact]
    public void TrailingNewlineFalse_OmitsFinalLineEnding()
    {
        var doc = new LrcDocumentBuilder()
            .AddLine("00:01.00", "x")
            .Build();
        var options = new LrcWriteOptions { TrailingNewline = false };
        var output = LrcWriter.Write(doc, options);
        output.ShouldBe("[00:01.00]x");
    }

    [Fact]
    public void MultiTimestampGroup_DefaultCollapsesToSingleEmission()
    {
        // AddLineGroup fans out into N LrcPlainLine objects sharing the same Text.
        // Default LrcWriteOptions has CollapseIdenticalLines = true, so the writer
        // re-folds them into a single [t1][t2]chorus emission.
        var t1 = LrcTimestamp.FromMilliseconds(1_000);
        var t2 = LrcTimestamp.FromMilliseconds(5_000);
        var doc = new LrcDocumentBuilder()
            .AddLineGroup([t1, t2], "chorus")
            .Build();
        var output = LrcWriter.Write(doc);
        output.ShouldBe("[00:01.00][00:05.00]chorus\n");
    }

    [Fact]
    public void MultiTimestampGroup_CollapseDisabled_EmitsOneLinePerTimestamp()
    {
        var t1 = LrcTimestamp.FromMilliseconds(1_000);
        var t2 = LrcTimestamp.FromMilliseconds(5_000);
        var doc = new LrcDocumentBuilder()
            .AddLineGroup([t1, t2], "chorus")
            .Build();
        var output = LrcWriter.Write(doc, new LrcWriteOptions { CollapseIdenticalLines = false });
        output.ShouldBe("[00:01.00]chorus\n[00:05.00]chorus\n");
    }

    [Fact]
    public void Milliseconds_TimestampPrecision_AppliesF()
    {
        var doc = new LrcDocumentBuilder()
            .AddLine(LrcTimestamp.FromMilliseconds(1_500), "x")
            .Build();
        var output = LrcWriter.Write(doc, new LrcWriteOptions { TimestampPrecision = LrcTimestampPrecision.Milliseconds });
        output.ShouldBe("[00:01.500]x\n");
    }
}
