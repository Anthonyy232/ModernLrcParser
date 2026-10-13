using ModernLrc.Model;

namespace ModernLrc.Tests.Unit.Model;

public sealed class LrcTimestampTests
{
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
    public void FromMilliseconds_TooLarge_Throws()
    {
        long tooLarge = long.MaxValue / TimeSpan.TicksPerMillisecond + 1;
        Should.Throw<ArgumentOutOfRangeException>(() => LrcTimestamp.FromMilliseconds(tooLarge));
    }

    [Fact]
    public void TotalMilliseconds_LargeAcceptedTimestamp_DoesNotOverflow()
    {
        long expected = (long)int.MaxValue + 123;
        LrcTimestamp.FromMilliseconds(expected).TotalMilliseconds.ShouldBe(expected);
    }

    [Fact]
    public void FromTimeSpan_Negative_Throws()
    {
        Should.Throw<ArgumentOutOfRangeException>(() =>
            LrcTimestamp.FromTimeSpan(TimeSpan.FromMilliseconds(-5)));
    }

    [Fact]
    public void Plus_TimeSpan_AddsCorrectly()
    {
        var sum = LrcTimestamp.FromMilliseconds(1_000) + TimeSpan.FromMilliseconds(500);
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
        TimeSpan diff = LrcTimestamp.FromMilliseconds(200) - LrcTimestamp.FromMilliseconds(500);
        diff.TotalMilliseconds.ShouldBe(-300);
    }

    [Fact]
    public void Comparison_OrdersByTicks()
    {
        var a = LrcTimestamp.FromMilliseconds(100);
        var b = LrcTimestamp.FromMilliseconds(200);
        (a < b).ShouldBeTrue();
        (b > a).ShouldBeTrue();
        (a <= b).ShouldBeTrue();
        (b >= a).ShouldBeTrue();
        a.CompareTo(b).ShouldBeLessThan(0);
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
        LrcTimestamp.Parse(input, null).Ticks.ShouldBe(expectedTicks);
    }

    [Theory]
    [InlineData("")]
    [InlineData("garbage")]
    [InlineData("-01:00.00")]
    [InlineData("01:60.00")]
    [InlineData("01:-1.00")]
    [InlineData("99999999999:00.00")] // past the minutes cap, which guards tick overflow
    public void TryParse_InvalidFormats_ReturnsFalse(string input)
    {
        LrcTimestamp.TryParse(input, null, out _).ShouldBeFalse();
    }

    [Fact]
    public void Parse_Null_Throws()
    {
        Should.Throw<ArgumentNullException>(() => LrcTimestamp.Parse((string)null!, null));
    }

    [Theory]
    [InlineData("G", "01:30.50")]
    [InlineData("F", "01:30.500")]
    [InlineData("S", "01:30")]
    [InlineData("B", "[01:30.50]")]
    [InlineData("W", "<01:30.50>")]
    public void ToString_FormatCodes_EmitExpected(string format, string expected)
    {
        LrcTimestamp.FromMilliseconds(90_500).ToString(format, null).ShouldBe(expected);
    }

    [Fact]
    public void ToString_UnknownFormat_Throws()
    {
        Should.Throw<FormatException>(() => LrcTimestamp.FromMilliseconds(1000).ToString("X", null));
    }

    [Fact]
    public void TryFormat_TooSmallBuffer_ReturnsFalseAndLeavesBufferUnchanged()
    {
        var t = LrcTimestamp.FromMilliseconds(90_500);
        Span<char> dest = ['X', 'Y', 'Z'];
        t.TryFormat(dest, out int written, "B", null).ShouldBeFalse();
        written.ShouldBe(0);
        dest.ToString().ShouldBe("XYZ");
    }

    [Fact]
    public void TryFormat_Utf8_WritesCorrectly()
    {
        Span<byte> dest = stackalloc byte[16];
        LrcTimestamp.FromMilliseconds(90_500).TryFormat(dest, out int written, "G", null).ShouldBeTrue();
        System.Text.Encoding.UTF8.GetString(dest[..written]).ShouldBe("01:30.50");
    }

    [Fact]
    public void TryFormat_Utf8_TooSmall_ReturnsFalse()
    {
        LrcTimestamp.FromMilliseconds(1000).TryFormat(new byte[4], out _, "G", null).ShouldBeFalse();
    }
}
