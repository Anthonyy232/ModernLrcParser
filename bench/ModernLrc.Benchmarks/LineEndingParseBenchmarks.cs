using BenchmarkDotNet.Attributes;
using ModernLrc;

namespace ModernLrc.Benchmarks;

/// <summary>Same logical document, parsed with LF and CRLF line endings.
/// The scanner runs an <c>IndexOfAny('\n','\r','&lt;')</c> SIMD scan per line; this
/// benchmark detects regressions where CRLF input takes a slower path than LF.</summary>
[MemoryDiagnoser]
[BenchmarkCategory("LineEnding")]
public class LineEndingParseBenchmarks
{
    private const int LineCount = 500;

    private string _lf = string.Empty;
    private string _crlf = string.Empty;

    [GlobalSetup]
    public void Setup()
    {
        var lfText = Sources.BuildText(LineCount, DocumentShape.Mixed);
        // Sources writes LF; force-convert for the CRLF case so total content is identical.
        _lf = lfText.Replace("\r\n", "\n", StringComparison.Ordinal);
        _crlf = _lf.Replace("\n", "\r\n", StringComparison.Ordinal);
    }

    [Benchmark(Baseline = true)]
    public LrcParseResult Parse_Lf() => LrcParser.Parse(_lf);

    [Benchmark]
    public LrcParseResult Parse_Crlf() => LrcParser.Parse(_crlf);
}
