using System.Text;
using ModernLrc;

namespace ModernLrc.Tests.Unit;

public sealed class LrcWriteOptionsTests
{
    [Fact]
    public void Default_HasExpectedDefaults()
    {
        var o = LrcWriteOptions.Default;
        o.LineEnding.ShouldBe(LrcLineEnding.Lf);
        o.Encoding.EncodingName.ShouldBe(Encoding.UTF8.EncodingName);
        o.EmitByteOrderMark.ShouldBeFalse();
        o.TrailingNewline.ShouldBeTrue();
        o.TimestampPrecision.ShouldBe(LrcTimestampPrecision.Centiseconds);
        o.CollapseIdenticalLines.ShouldBeTrue();
        o.EmitVoiceMarkers.ShouldBeTrue();
        o.VoiceMarkerOnChangeOnly.ShouldBeTrue();
        o.MetadataOrdering.ShouldBe(LrcMetadataOrdering.Canonical);
    }
}
