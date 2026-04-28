using BenchmarkDotNet.Attributes;
using ModernLrc.Benchmarks.Comparison.Adapters;

namespace ModernLrc.Benchmarks.Comparison;

// Only libraries with a serialize-to-string API are included.
// Kfstorm and SharpLyrics are read-only; absence here is the answer.
[MemoryDiagnoser]
public class WriteComparisonBenchmarks
{
    [ParamsSource(nameof(Inputs))]
    public NamedInput Input { get; set; } = Corpus.Small;

    public static IEnumerable<NamedInput> Inputs => Corpus.All;

    private object _modernParsed = null!;
    private object _karaokeParsed = null!;
    private object _opportunityParsed = null!;

    [GlobalSetup]
    public void Setup()
    {
        _modernParsed = ModernLrcAdapter.Parse(Input.Text);
        _karaokeParsed = KaraokeDevAdapter.Parse(Input.Text);
        _opportunityParsed = OpportunityLiuAdapter.Parse(Input.Text);
    }

    [Benchmark(Baseline = true)]
    public string Lib_ModernLrc() => ModernLrcAdapter.Write(_modernParsed);

    [Benchmark]
    public string Lib_KaraokeDev() => KaraokeDevAdapter.Write(_karaokeParsed);

    [Benchmark]
    public string Lib_OpportunityLiu() => OpportunityLiuAdapter.Write(_opportunityParsed);
}
