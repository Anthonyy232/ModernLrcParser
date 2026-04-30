using BenchmarkDotNet.Attributes;
using ModernLrc.Model;

namespace ModernLrc.Benchmarks;

/// <summary>Hot inner-loop micro-benchmark for <see cref="LrcTimestamp.TryParse(ReadOnlySpan{char}, IFormatProvider, out LrcTimestamp)"/>.
/// Each call should be sub-microsecond; tracked here to catch regressions in the parsing fast path.</summary>
[MemoryDiagnoser]
[BenchmarkCategory("Timestamp")]
public class TimestampBenchmarks
{
    private const string Canonical = "01:23.45";
    private const string ThreeDigit = "01:23.456";
    private const string Hours = "1:02:03.45";
    private const string CommaDecimal = "01:23,45";
    private const string ColonFrac = "01:23:45";
    private const string NoFraction = "01:23";

    [Benchmark(Baseline = true)]
    public LrcTimestamp TryParse_Canonical()
    {
        LrcTimestamp.TryParse(Canonical.AsSpan(), null, out var ts);
        return ts;
    }

    [Benchmark]
    public LrcTimestamp TryParse_ThreeDigit()
    {
        LrcTimestamp.TryParse(ThreeDigit.AsSpan(), null, out var ts);
        return ts;
    }

    [Benchmark]
    public LrcTimestamp TryParse_Hours()
    {
        LrcTimestamp.TryParse(Hours.AsSpan(), null, out var ts);
        return ts;
    }

    [Benchmark]
    public LrcTimestamp TryParse_CommaDecimal()
    {
        LrcTimestamp.TryParse(CommaDecimal.AsSpan(), null, out var ts);
        return ts;
    }

    [Benchmark]
    public LrcTimestamp TryParse_ColonFraction()
    {
        LrcTimestamp.TryParse(ColonFrac.AsSpan(), null, out var ts);
        return ts;
    }

    [Benchmark]
    public LrcTimestamp TryParse_NoFraction()
    {
        LrcTimestamp.TryParse(NoFraction.AsSpan(), null, out var ts);
        return ts;
    }

    // ---- Formatting ----
    // TryFormat is on the per-timestamp render hot path. Both char (CharBufferWriter)
    // and UTF-8 (Utf8BufferWriter) overloads are measured because they take different
    // code paths internally.

    private static readonly LrcTimestamp Sample = LrcTimestamp.FromMilliseconds(83_450);

    [Benchmark]
    public int TryFormat_Char_GeneralFormat()
    {
        Span<char> dest = stackalloc char[24];
        Sample.TryFormat(dest, out int written, "G".AsSpan(), null);
        return written;
    }

    [Benchmark]
    public int TryFormat_Char_MillisecondFormat()
    {
        Span<char> dest = stackalloc char[24];
        Sample.TryFormat(dest, out int written, "F".AsSpan(), null);
        return written;
    }

    [Benchmark]
    public int TryFormat_Utf8_GeneralFormat()
    {
        Span<byte> dest = stackalloc byte[24];
        Sample.TryFormat(dest, out int written, "G".AsSpan(), null);
        return written;
    }
}
