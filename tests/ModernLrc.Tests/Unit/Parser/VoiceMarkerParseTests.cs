using ModernLrc;
using ModernLrc.Model;

namespace ModernLrc.Tests.Unit.Parser;

public sealed class VoiceMarkerParseTests
{
    [Fact]
    public void MaleMarker_SetsEffectiveVoice()
    {
        var result = LrcParser.Parse("[00:01.00]M: hello");
        var line = (LrcPlainLine)result.Document.Lines[0];
        line.EffectiveVoice.ShouldBe(LrcVoice.Male);
        line.Text.ShouldBe("hello");
    }

    [Fact]
    public void FemaleMarker_PropagatesToSubsequentLines()
    {
        var result = LrcParser.Parse(
            "[00:01.00]F: she sings\n[00:02.00]still hers");
        var line0 = (LrcPlainLine)result.Document.Lines[0];
        var line1 = (LrcPlainLine)result.Document.Lines[1];
        line0.EffectiveVoice.ShouldBe(LrcVoice.Female);
        line1.EffectiveVoice.ShouldBe(LrcVoice.Female);
    }

    [Fact]
    public void DuetMarker_Recognized()
    {
        var result = LrcParser.Parse("[00:01.00]D: together");
        ((LrcPlainLine)result.Document.Lines[0]).EffectiveVoice.ShouldBe(LrcVoice.Duet);
    }

    [Fact]
    public void NoSpaceAfterColon_NotRecognizedAsMarker()
    {
        // "M:hello" without the trailing space is plain text, not a marker.
        var result = LrcParser.Parse("[00:01.00]M:hello");
        var line = (LrcPlainLine)result.Document.Lines[0];
        line.EffectiveVoice.ShouldBe(LrcVoice.Default);
        line.Text.ShouldBe("M:hello");
    }
}
