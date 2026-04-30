#pragma warning disable CA1305 // LrcTimestamp.Parse always uses InvariantCulture internally; the IFormatProvider overload is irrelevant to the test intent
using ModernLrc.Model;

namespace ModernLrc.Tests.Unit.Model;

public sealed class LrcTimestampTests
{
    [Fact]
    public void Zero_HasZeroTicks()
    {
        LrcTimestamp.Zero.Ticks.ShouldBe(0L);
        LrcTimestamp.Zero.TotalMilliseconds.ShouldBe(0);
        LrcTimestamp.Zero.TotalSeconds.ShouldBe(0.0);
    }

    [Fact]
    public void FromTicks_NonNegative_Roundtrips()
    {
        var t = LrcTimestamp.FromTicks(12_345);
        t.Ticks.ShouldBe(12_345L);
    }

    [Fact]
    public void FromTicks_Negative_Throws()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => LrcTimestamp.FromTicks(-1));
    }

    [Fact]
    public void FromMilliseconds_Negative_Throws()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => LrcTimestamp.FromMilliseconds(-1));
    }

    [Fact]
    public void FromMilliseconds_OneSecond_Equals10MTicks()
    {
        LrcTimestamp.FromMilliseconds(1_000).Ticks.ShouldBe(TimeSpan.TicksPerSecond);
    }

    [Fact]
    public void FromMilliseconds_TooLarge_Throws()
    {
        long tooLarge = long.MaxValue / TimeSpan.TicksPerMillisecond + 1;
        Should.Throw<ArgumentOutOfRangeException>(() => LrcTimestamp.FromMilliseconds(tooLarge));
    }

    [Fact]
    public void FromTimeSpan_Negative_Throws()
    {
        Should.Throw<ArgumentOutOfRangeException>(() =>
            LrcTimestamp.FromTimeSpan(TimeSpan.FromMilliseconds(-5)));
    }

    [Fact]
    public void FromTimeSpan_RoundtripsViaImplicitOperator()
    {
        var ts = TimeSpan.FromMilliseconds(2_500);
        TimeSpan back = LrcTimestamp.FromTimeSpan(ts);
        back.ShouldBe(ts);
    }

    [Fact]
    public void Plus_TimeSpan_AddsCorrectly()
    {
        var t = LrcTimestamp.FromMilliseconds(1_000);
        var sum = t + TimeSpan.FromMilliseconds(500);
        sum.TotalMilliseconds.ShouldBe(1_500);
    }

    [Fact]
    public void Minus_TimeSpan_GoingNegative_Throws()
    {
        var t = LrcTimestamp.FromMilliseconds(100);
        Should.Throw<OverflowException>(() => _ = t - TimeSpan.FromMilliseconds(500));
    }

    [Fact]
    public void Minus_LrcTimestamp_ProducesSignedTimeSpan()
    {
        var a = LrcTimestamp.FromMilliseconds(200);
        var b = LrcTimestamp.FromMilliseconds(500);
        TimeSpan diff = a - b;
        diff.TotalMilliseconds.ShouldBe(-300);
    }

    [Fact]
    public void Comparison_OrdersByTicks()
    {
        var a = LrcTimestamp.FromMilliseconds(100);
        var b = LrcTimestamp.FromMilliseconds(200);
        (a < b).ShouldBeTrue();
        (b > a).ShouldBeTrue();
#pragma warning disable CS1718 // Comparison made to same variable — intentional: testing reflexive <=
        (a <= a).ShouldBeTrue();
#pragma warning restore CS1718
        (b >= a).ShouldBeTrue();
        a.CompareTo(b).ShouldBeLessThan(0);
    }

    [Fact]
    public void Equality_ByTicks()
    {
        var a = LrcTimestamp.FromMilliseconds(123);
        var b = LrcTimestamp.FromMilliseconds(123);
        (a == b).ShouldBeTrue();
        a.GetHashCode().ShouldBe(b.GetHashCode());
    }

    [Theory]
    [InlineData("00:00.00", 0L)]
    [InlineData("00:01.00", TimeSpan.TicksPerSecond)]
    [InlineData("01:30.50", 90L * TimeSpan.TicksPerSecond + 500L * TimeSpan.TicksPerMillisecond)]
    [InlineData("00:12.345", 12L * TimeSpan.TicksPerSecond + 345L * TimeSpan.TicksPerMillisecond)]
    [InlineData("00:12", 12L * TimeSpan.TicksPerSecond)]
    [InlineData("00:12:34", 12L * TimeSpan.TicksPerSecond + 340L * TimeSpan.TicksPerMillisecond)]
    [InlineData("123:45.67", 123L * TimeSpan.TicksPerMinute + 45L * TimeSpan.TicksPerSecond + 670L * TimeSpan.TicksPerMillisecond)]
    public void Parse_ValidFormats_ReturnsExpectedTicks(string input, long expectedTicks)
    {
        var t = LrcTimestamp.Parse(input);
        t.Ticks.ShouldBe(expectedTicks);
    }

    [Theory]
    [InlineData("")]
    [InlineData("garbage")]
    [InlineData("-01:00.00")]
    [InlineData("01:60.00")]
    [InlineData("01:-1.00")]
    public void TryParse_InvalidFormats_ReturnsFalse(string input)
    {
        LrcTimestamp.TryParse(input, null, out _).ShouldBeFalse();
    }

    [Theory]
    [InlineData("99999999999:00.00")] // ~10^11 minutes — well past the sanity cap (~5.2*10^7).
    [InlineData("100000000000:00.00")]
    public void TryParse_AbsurdMinutes_ReturnsFalse_NoSilentOverflow(string input)
    {
        // Without the sanity cap, minutes * TicksPerMinute would silently wrap to negative
        // and produce an invalid LrcTimestamp. The cap rejects the input cleanly.
        LrcTimestamp.TryParse(input, null, out _).ShouldBeFalse();
    }

    [Fact]
    public void Parse_Null_Throws()
    {
        Should.Throw<ArgumentNullException>(() => LrcTimestamp.Parse((string)null!));
    }

    [Fact]
    public void ToString_DefaultIsCentiseconds()
    {
        var t = LrcTimestamp.FromMilliseconds(90_500);
        t.ToString().ShouldBe("01:30.50");
    }

    [Theory]
    [InlineData("G", "01:30.50")]
    [InlineData("F", "01:30.500")]
    [InlineData("S", "01:30")]
    [InlineData("B", "[01:30.50]")]
    [InlineData("W", "<01:30.50>")]
    public void ToString_FormatCodes_EmitExpected(string format, string expected)
    {
        var t = LrcTimestamp.FromMilliseconds(90_500);
        t.ToString(format, System.Globalization.CultureInfo.InvariantCulture).ShouldBe(expected);
    }

    [Fact]
    public void TryFormat_Char_WritesCorrectly()
    {
        var t = LrcTimestamp.FromMilliseconds(90_500);
        Span<char> dest = stackalloc char[16];
        t.TryFormat(dest, out int written, "G", null).ShouldBeTrue();
        dest[..written].ToString().ShouldBe("01:30.50");
    }

    [Fact]
    public void TryFormat_Char_TooSmall_ReturnsFalse()
    {
        var t = LrcTimestamp.FromMilliseconds(90_500);
        Span<char> dest = stackalloc char[3];
        t.TryFormat(dest, out int written, "G", null).ShouldBeFalse();
        written.ShouldBe(0);
    }

    [Fact]
    public void TryFormat_Utf8_WritesCorrectly()
    {
        var t = LrcTimestamp.FromMilliseconds(90_500);
        Span<byte> dest = stackalloc byte[16];
        t.TryFormat(dest, out int written, "G", null).ShouldBeTrue();
        System.Text.Encoding.UTF8.GetString(dest[..written]).ShouldBe("01:30.50");
    }

    [Fact]
    public void RoundTrip_ParseEqualsToString_G()
    {
        var t = LrcTimestamp.FromMilliseconds(90_500);
        var formatted = t.ToString("G", System.Globalization.CultureInfo.InvariantCulture);
        LrcTimestamp.Parse(formatted).ShouldBe(t);
    }

    [Fact]
    public void RoundTrip_ParseEqualsToString_F()
    {
        var t = LrcTimestamp.FromMilliseconds(90_507);
        var formatted = t.ToString("F", System.Globalization.CultureInfo.InvariantCulture);
        LrcTimestamp.Parse(formatted).ShouldBe(t);
    }

    [Fact]
    public void TryFormat_BFormat_TooSmallBuffer_LeavesBufferUnchanged()
    {
        var t = LrcTimestamp.FromMilliseconds(90_500);
        Span<char> dest = stackalloc char[3];
        dest[0] = 'X';
        dest[1] = 'Y';
        dest[2] = 'Z';
        t.TryFormat(dest, out int written, "B", null).ShouldBeFalse();
        written.ShouldBe(0);
        // Buffer must be unchanged on failure (ISpanFormattable contract).
        dest[0].ShouldBe('X');
        dest[1].ShouldBe('Y');
        dest[2].ShouldBe('Z');
    }
}
