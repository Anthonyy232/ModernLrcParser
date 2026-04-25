using BenchmarkDotNet.Attributes;
using ModernLrc;

namespace ModernLrc.Benchmarks;

/// <summary>Worst-case recovery cost: tolerant parse over an error-rich document.
/// Confirms diagnostic-emission overhead stays linear and that strict-mode short-circuits.</summary>
[MemoryDiagnoser]
[BenchmarkCategory("Recovery")]
public class RecoveryBenchmarks
{
    private const int LineCount = 1000;
    private const int BadLineEveryNth = 10; // ~10% malformed

    private string _clean = string.Empty;
    private string _errorRich = string.Empty;
    private LrcParseOptions _strict = LrcParseOptions.Default;
    private LrcParseOptions _highCap = LrcParseOptions.Default;

    [GlobalSetup]
    public void Setup()
    {
        _clean = Sources.BuildText(LineCount, DocumentShape.Simple);
        _errorRich = Sources.BuildErrorRichText(LineCount, BadLineEveryNth);
        _strict = LrcParseOptions.Default with { Strictness = LrcStrictness.Strict };
        _highCap = LrcParseOptions.Default with { MaxDiagnostics = 4096 };
    }

    [Benchmark(Baseline = true)]
    public LrcParseResult Tolerant_Clean() => LrcParser.Parse(_clean);

    [Benchmark]
    public LrcParseResult Tolerant_ErrorRich() => LrcParser.Parse(_errorRich);

    [Benchmark]
    public LrcParseResult Tolerant_ErrorRich_HighDiagCap() => LrcParser.Parse(_errorRich, _highCap);

    [Benchmark]
    public bool Strict_ErrorRich_FirstErrorThrows()
    {
        try { LrcParser.Parse(_errorRich, _strict); return false; }
        catch (LrcParseException) { return true; }
    }
}
