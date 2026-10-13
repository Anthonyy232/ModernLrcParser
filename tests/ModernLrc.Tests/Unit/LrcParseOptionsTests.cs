using System.Text;
using ModernLrc;

namespace ModernLrc.Tests.Unit;

public sealed class LrcParseOptionsTests
{
    [Fact]
    public void Default_HasExpectedDefaults()
    {
        var o = LrcParseOptions.Default;
        o.Strictness.ShouldBe(LrcStrictness.Tolerant);
        o.Encoding.ShouldBeNull();
        o.FallbackEncoding.ShouldBe(Encoding.Latin1);
        o.MaxDiagnostics.ShouldBe(256);
        o.ImplausibleTimestampThreshold.ShouldBe(TimeSpan.FromHours(24));
    }

    [Fact]
    public void MaxDiagnostics_Negative_Throws()
    {
        Should.Throw<ArgumentOutOfRangeException>(() =>
            new LrcParseOptions { MaxDiagnostics = -1 });
    }
}
