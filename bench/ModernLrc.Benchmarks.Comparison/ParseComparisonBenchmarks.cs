using BenchmarkDotNet.Attributes;
using Kfstorm.LrcParser;
using Opportunity.LrcParser;
using SharpLyrics;
using KaraokeLrcParser = LrcParser.Parser.Lrc.LrcParser;

namespace ModernLrc.Benchmarks.Comparison;

[MemoryDiagnoser]
public class ParseComparisonBenchmarks
{
    private static readonly KaraokeLrcParser Karaoke = new();

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
    public void Cleanup() => File.Delete(_tempFile);

    [Benchmark(Baseline = true)]
    public object Lib_ModernLrc() => LrcParser.Parse(Input.Text);

    [Benchmark]
    public object Lib_Kfstorm() => LrcFile.FromText(Input.Text);

    [Benchmark]
    public object Lib_KaraokeDev() => Karaoke.Decode(Input.Text);

    [Benchmark]
    public object Lib_OpportunityLiu() => Lyrics.Parse(Input.Text);

    // Reads files only
    [Benchmark]
    public object Lib_SharpLyrics() => LyricReader.GetLyrics(_tempFile);
}
