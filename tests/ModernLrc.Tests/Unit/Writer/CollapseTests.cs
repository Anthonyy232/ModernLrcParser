using ModernLrc;
using ModernLrc.Model;

namespace ModernLrc.Tests.Unit.Writer;

public sealed class CollapseTests
{
    [Fact]
    public void IdenticalConsecutive_Collapsed_WhenOptionEnabled()
    {
        var doc = new LrcDocumentBuilder()
            .AddLine("00:01.00", "chorus")
            .AddLine("00:02.00", "chorus")
            .Build();
        var options = new LrcWriteOptions { CollapseIdenticalLines = true };
        LrcWriter.Write(doc, options).ShouldBe("[00:01.00][00:02.00]chorus\n");
    }

    [Fact]
    public void DifferentText_NotCollapsed()
    {
        var doc = new LrcDocumentBuilder()
            .AddLine("00:01.00", "a")
            .AddLine("00:02.00", "b")
            .Build();
        var options = new LrcWriteOptions { CollapseIdenticalLines = true };
        LrcWriter.Write(doc, options).ShouldBe("[00:01.00]a\n[00:02.00]b\n");
    }

    [Fact]
    public void DifferentVoice_NotCollapsed()
    {
        var doc = new LrcDocumentBuilder()
            .AddLine("00:01.00", "x", LrcVoice.Male)
            .AddLine("00:02.00", "x", LrcVoice.Female)
            .Build();
        var options = new LrcWriteOptions { CollapseIdenticalLines = true };
        LrcWriter.Write(doc, options).ShouldNotContain("[00:01.00][00:02.00]");
    }

    [Fact]
    public void OptionDisabled_NoCollapsing()
    {
        var doc = new LrcDocumentBuilder()
            .AddLine("00:01.00", "x")
            .AddLine("00:02.00", "x")
            .Build();
        LrcWriter.Write(doc).ShouldBe("[00:01.00]x\n[00:02.00]x\n");
    }
}
