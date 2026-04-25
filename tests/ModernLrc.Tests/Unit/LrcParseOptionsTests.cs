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
        o.FallbackEncoding.ShouldBe(Encoding.UTF8);
        o.MaxDiagnostics.ShouldBe(256);
        o.ImplausibleTimestampThreshold.ShouldBe(TimeSpan.FromHours(24));
        o.ReadBufferSize.ShouldBe(4096);
    }

    [Fact]
    public void MaxDiagnostics_Negative_Throws()
    {
        Should.Throw<ArgumentOutOfRangeException>(() =>
            new LrcParseOptions { MaxDiagnostics = -1 });
    }

    [Fact]
    public void MaxDiagnostics_Zero_Allowed()
    {
        var o = new LrcParseOptions { MaxDiagnostics = 0 };
        o.MaxDiagnostics.ShouldBe(0);
    }

    [Fact]
    public void ReadBufferSize_TooSmall_Throws()
    {
        Should.Throw<ArgumentOutOfRangeException>(() =>
            new LrcParseOptions { ReadBufferSize = 32 });
    }

    [Fact]
    public void ReadBufferSize_AtMinimum_Allowed()
    {
        var o = new LrcParseOptions { ReadBufferSize = 64 };
        o.ReadBufferSize.ShouldBe(64);
    }

    [Fact]
    public void Init_OverridesAreReflected()
    {
        var o = new LrcParseOptions
        {
            Strictness = LrcStrictness.Strict,
            Encoding = Encoding.Unicode,
            FallbackEncoding = null,
            MaxDiagnostics = 10,
            ImplausibleTimestampThreshold = TimeSpan.FromHours(48),
            ReadBufferSize = 8192,
        };
        o.Strictness.ShouldBe(LrcStrictness.Strict);
        o.Encoding.ShouldBe(Encoding.Unicode);
        o.FallbackEncoding.ShouldBeNull();
        o.MaxDiagnostics.ShouldBe(10);
        o.ImplausibleTimestampThreshold.ShouldBe(TimeSpan.FromHours(48));
        o.ReadBufferSize.ShouldBe(8192);
    }
}
