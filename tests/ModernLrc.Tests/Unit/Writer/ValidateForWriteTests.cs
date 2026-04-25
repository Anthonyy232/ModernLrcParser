using ModernLrc;
using ModernLrc.Diagnostics;
using ModernLrc.Model;

namespace ModernLrc.Tests.Unit.Writer;

public sealed class ValidateForWriteTests
{
    [Fact]
    public void NoVoice_ReturnsEmpty()
    {
        var doc = new LrcDocumentBuilder().AddLine("00:01.00", "x").Build();
        LrcWriter.ValidateForWrite(doc).ShouldBeEmpty();
    }

    [Fact]
    public void DefaultAfterFemale_EmitsLRC0090()
    {
        var doc = new LrcDocumentBuilder()
            .AddLine("00:01.00", "she", LrcVoice.Female)
            .AddLine("00:02.00", "untimed")  // EffectiveVoice = Default
            .Build();
        var diagnostics = LrcWriter.ValidateForWrite(doc);
        diagnostics.ShouldContain(d => d.Code == LrcDiagnosticIds.UnrepresentableVoiceTransition);
    }

    [Fact]
    public void EmitVoiceMarkersFalse_NoValidationDiagnostics()
    {
        var doc = new LrcDocumentBuilder()
            .AddLine("00:01.00", "she", LrcVoice.Female)
            .AddLine("00:02.00", "untimed")
            .Build();
        var options = new LrcWriteOptions { EmitVoiceMarkers = false };
        LrcWriter.ValidateForWrite(doc, options).ShouldBeEmpty();
    }
}
