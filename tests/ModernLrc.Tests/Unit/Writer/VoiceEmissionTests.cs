using ModernLrc;
using ModernLrc.Model;

namespace ModernLrc.Tests.Unit.Writer;

public sealed class VoiceEmissionTests
{
    [Fact]
    public void MaleVoice_EmitsMarker()
    {
        var doc = new LrcDocumentBuilder()
            .AddLine("00:01.00", "x", LrcVoice.Male)
            .Build();
        LrcWriter.Write(doc).ShouldBe("[00:01.00]M: x\n");
    }

    [Fact]
    public void VoiceMarkerOnChangeOnly_FemaleThenFemale_OnlyOnce()
    {
        var doc = new LrcDocumentBuilder()
            .AddLine("00:01.00", "a", LrcVoice.Female)
            .AddLine("00:02.00", "b", LrcVoice.Female)
            .Build();
        var output = LrcWriter.Write(doc);
        output.ShouldBe("[00:01.00]F: a\n[00:02.00]b\n");
    }

    [Fact]
    public void VoiceMarkerOnChangeOnlyFalse_AlwaysEmitsMarker()
    {
        var doc = new LrcDocumentBuilder()
            .AddLine("00:01.00", "a", LrcVoice.Female)
            .AddLine("00:02.00", "b", LrcVoice.Female)
            .Build();
        var options = new LrcWriteOptions { VoiceMarkerOnChangeOnly = false };
        LrcWriter.Write(doc, options).ShouldBe("[00:01.00]F: a\n[00:02.00]F: b\n");
    }

    [Fact]
    public void EmitVoiceMarkersFalse_NoMarkers()
    {
        var doc = new LrcDocumentBuilder()
            .AddLine("00:01.00", "x", LrcVoice.Female)
            .Build();
        var options = new LrcWriteOptions { EmitVoiceMarkers = false };
        LrcWriter.Write(doc, options).ShouldBe("[00:01.00]x\n");
    }

    [Fact]
    public void VoiceTransition_MaleToFemale_EmitsBothMarkers()
    {
        var doc = new LrcDocumentBuilder()
            .AddLine("00:01.00", "his", LrcVoice.Male)
            .AddLine("00:02.00", "hers", LrcVoice.Female)
            .Build();
        LrcWriter.Write(doc).ShouldBe("[00:01.00]M: his\n[00:02.00]F: hers\n");
    }

    [Fact]
    public void DefaultAfterNonDefault_EmitsNoMarker_TrackerNotUpdated()
    {
        var doc = new LrcDocumentBuilder()
            .AddLine("00:01.00", "her line", LrcVoice.Female)
            .AddLine("00:02.00", "no voice")  // EffectiveVoice = Default
            .Build();
        // The Default-after-Female line emits no marker; the next non-default
        // line will still emit a marker because tracker wasn't updated.
        var output = LrcWriter.Write(doc);
        output.ShouldBe("[00:01.00]F: her line\n[00:02.00]no voice\n");
    }
}
