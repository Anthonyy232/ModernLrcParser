using BenchmarkDotNet.Attributes;
using ModernLrc;

namespace ModernLrc.Benchmarks;

/// <summary>Realistic end-to-end workflow: text → parse → document → write → text.
/// Fidelity is exercised by the <see cref="DocumentShape.Mixed"/> shape.</summary>
[MemoryDiagnoser]
[BenchmarkCategory("Roundtrip")]
public class RoundtripBenchmarks
{
    [Params(10, 100, 1000)]
    public int LineCount { get; set; }

    private string _text = string.Empty;

    [GlobalSetup]
    public void Setup() => _text = Sources.BuildText(LineCount, DocumentShape.Mixed);

    [Benchmark]
    public string Parse_Then_Write_String()
    {
        var doc = LrcParser.Parse(_text).Document;
        return LrcWriter.Write(doc);
    }
}
