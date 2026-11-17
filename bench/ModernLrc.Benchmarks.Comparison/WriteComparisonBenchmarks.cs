using BenchmarkDotNet.Attributes;
using LrcParser.Model;
using Opportunity.LrcParser;
using KaraokeLrcParser = LrcParser.Parser.Lrc.LrcParser;

namespace ModernLrc.Benchmarks.Comparison;

// Only libraries with a serialize-to-string API are included.
// Kfstorm and SharpLyrics are read-only; absence here is the answer.
[MemoryDiagnoser]
public class WriteComparisonBenchmarks
{
    private static readonly KaraokeLrcParser Karaoke = new();

    [ParamsSource(nameof(Inputs))]
    public NamedInput Input { get; set; } = Corpus.Small;

    public static IEnumerable<NamedInput> Inputs => Corpus.All;

    private LrcDocument _modern = LrcDocument.Empty;
    private Song _karaoke = null!;
    private IParseResult<Line> _opportunity = null!;

    [GlobalSetup]
    public void Setup()
    {
        _modern = LrcParser.Parse(Input.Text).Document;
        _karaoke = Karaoke.Decode(Input.Text);
        _opportunity = Lyrics.Parse(Input.Text);
    }

    [Benchmark(Baseline = true)]
    public string Lib_ModernLrc() => LrcWriter.Write(_modern);

    [Benchmark]
    public string Lib_KaraokeDev() => Karaoke.Encode(_karaoke);

    [Benchmark]
    public string Lib_OpportunityLiu() => _opportunity.Lyrics.ToString();
}
