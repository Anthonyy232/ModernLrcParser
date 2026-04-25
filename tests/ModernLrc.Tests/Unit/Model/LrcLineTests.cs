using ModernLrc.Model;

namespace ModernLrc.Tests.Unit.Model;

public sealed class LrcLineTests
{
    [Fact]
    public void LrcPlainLine_RequiredProperties_AssignedViaInit()
    {
        var line = new LrcPlainLine
        {
            Timestamps = [LrcTimestamp.FromMilliseconds(1_000)],
            Text = "hello world",
        };
        line.Timestamps.Count.ShouldBe(1);
        line.Text.ShouldBe("hello world");
        line.EffectiveVoice.ShouldBe(LrcVoice.Default);
    }

    [Fact]
    public void LrcPlainLine_WithVoice_PreservesIt()
    {
        var line = new LrcPlainLine
        {
            Timestamps = [LrcTimestamp.FromMilliseconds(0)],
            Text = "x",
            EffectiveVoice = LrcVoice.Female,
        };
        line.EffectiveVoice.ShouldBe(LrcVoice.Female);
    }

    [Fact]
    public void LrcPlainLine_ContentEquality_HoldsAcrossInstances()
    {
        var t = LrcTimestamp.FromMilliseconds(1_000);
        var a = new LrcPlainLine { Timestamps = [t], Text = "x" };
        var b = new LrcPlainLine { Timestamps = [t], Text = "x" };
        a.ShouldBe(b);
        a.GetHashCode().ShouldBe(b.GetHashCode());
    }

    [Fact]
    public void LrcPlainLine_DifferentText_NotEqual()
    {
        var t = LrcTimestamp.FromMilliseconds(1_000);
        var a = new LrcPlainLine { Timestamps = [t], Text = "x" };
        var b = new LrcPlainLine { Timestamps = [t], Text = "y" };
        a.ShouldNotBe(b);
    }

    [Fact]
    public void LrcEnhancedLine_RequiredProperties_AssignedViaInit()
    {
        var t0 = LrcTimestamp.FromMilliseconds(0);
        var t1 = LrcTimestamp.FromMilliseconds(500);
        var line = new LrcEnhancedLine
        {
            Timestamps = [t0],
            Words = [new(t0, "hello "), new(t1, "world")],
        };
        line.Words.Count.ShouldBe(2);
        line.Words[1].Text.ShouldBe("world");
    }

    [Fact]
    public void LrcEnhancedLine_ContentEquality_DeepCompares()
    {
        var t = LrcTimestamp.FromMilliseconds(0);
        var a = new LrcEnhancedLine { Timestamps = [t], Words = [new(t, "x")] };
        var b = new LrcEnhancedLine { Timestamps = [t], Words = [new(t, "x")] };
        a.ShouldBe(b);
    }

    [Fact]
    public void LrcLine_ExhaustiveSwitch_CoversBothSubtypes()
    {
        LrcLine line = new LrcPlainLine
        {
            Timestamps = [LrcTimestamp.Zero],
            Text = "x",
        };
        // CS8509: compiler cannot prove the sealed private-constructor hierarchy is exhaustive;
        // suppress rather than add a dead-code arm that would hide future subtype additions.
#pragma warning disable CS8509
        var label = line switch
        {
            LrcPlainLine => "plain",
            LrcEnhancedLine => "enhanced",
        };
#pragma warning restore CS8509
        label.ShouldBe("plain");
    }
}
