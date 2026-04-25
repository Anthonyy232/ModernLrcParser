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
        o.CollapseIdenticalLines.ShouldBeFalse();
        o.EmitVoiceMarkers.ShouldBeTrue();
        o.VoiceMarkerOnChangeOnly.ShouldBeTrue();
        o.MetadataOrdering.ShouldBe(LrcMetadataOrdering.Canonical);
        o.InitialBufferSize.ShouldBe(4096);
    }

    [Fact]
    public void Default_EncodingIsUtf8WithoutBom()
    {
        var o = LrcWriteOptions.Default;
        o.Encoding.GetPreamble().Length.ShouldBe(0);
    }

    [Fact]
    public void InitialBufferSize_TooSmall_Throws()
    {
        Should.Throw<ArgumentOutOfRangeException>(() =>
            new LrcWriteOptions { InitialBufferSize = 8 });
    }

    [Fact]
    public void InitialBufferSize_AtMinimum_Allowed()
    {
        var o = new LrcWriteOptions { InitialBufferSize = 16 };
        o.InitialBufferSize.ShouldBe(16);
    }

    [Fact]
    public void Init_OverridesAreReflected()
    {
        var o = new LrcWriteOptions
        {
            LineEnding = LrcLineEnding.Crlf,
            EmitByteOrderMark = true,
            TrailingNewline = false,
            TimestampPrecision = LrcTimestampPrecision.Milliseconds,
            CollapseIdenticalLines = true,
            EmitVoiceMarkers = false,
            VoiceMarkerOnChangeOnly = false,
            MetadataOrdering = LrcMetadataOrdering.Alphabetical,
            InitialBufferSize = 65_536,
        };
        o.LineEnding.ShouldBe(LrcLineEnding.Crlf);
        o.EmitByteOrderMark.ShouldBeTrue();
        o.TrailingNewline.ShouldBeFalse();
        o.TimestampPrecision.ShouldBe(LrcTimestampPrecision.Milliseconds);
        o.CollapseIdenticalLines.ShouldBeTrue();
        o.EmitVoiceMarkers.ShouldBeFalse();
        o.VoiceMarkerOnChangeOnly.ShouldBeFalse();
        o.MetadataOrdering.ShouldBe(LrcMetadataOrdering.Alphabetical);
        o.InitialBufferSize.ShouldBe(65_536);
    }
}
