using BenchmarkDotNet.Attributes;
using ModernLrc.Benchmarks.Comparison.Adapters;

namespace ModernLrc.Benchmarks.Comparison;

[MemoryDiagnoser]
public class ParseComparisonBenchmarks
{
    [ParamsSource(nameof(Inputs))]
    public NamedInput Input { get; set; } = Corpus.Small;

    public static IEnumerable<NamedInput> Inputs => Corpus.All;

    private string _tempFile = string.Empty;

    [GlobalSetup]
    public void Setup()
    {
        _tempFile = Path.Combine(Path.GetTempPath(),
            $"modernlrc-bench-{Input.Name}-{Guid.NewGuid():N}.lrc");
        File.WriteAllText(_tempFile, Input.Text);
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        if (File.Exists(_tempFile))
            File.Delete(_tempFile);
    }

    [Benchmark(Baseline = true)]
    public object Lib_ModernLrc() => ModernLrcAdapter.Parse(Input.Text);

    [Benchmark]
    public object Lib_Kfstorm() => KfstormAdapter.Parse(Input.Text);

    [Benchmark]
    public object Lib_KaraokeDev() => KaraokeDevAdapter.Parse(Input.Text);

    [Benchmark]
    public object Lib_OpportunityLiu() => OpportunityLiuAdapter.Parse(Input.Text);

    [Benchmark]
    public object Lib_SharpLyrics() => SharpLyricsAdapter.Parse(_tempFile);
}
