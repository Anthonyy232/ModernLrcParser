using System.Text;
using BenchmarkDotNet.Attributes;
using ModernLrc;

namespace ModernLrc.Benchmarks;

/// <summary>Async parse and write paths. In-memory streams only — no disk I/O so the
/// numbers reflect overhead added by async machinery (state-machine, ConfigureAwait,
/// ValueTask), not file-system noise.</summary>
[MemoryDiagnoser]
[CategoriesColumn]
[BenchmarkCategory("Async")]
public class AsyncBenchmarks
{
    [Params(100, 1000)]
    public int LineCount { get; set; }

    private string _text = string.Empty;
    private byte[] _bytes = Array.Empty<byte>();
    private LrcDocument _doc = LrcDocument.Empty;

    [GlobalSetup]
    public void Setup()
    {
        _text = Sources.BuildText(LineCount, DocumentShape.Mixed);
        _bytes = Encoding.UTF8.GetBytes(_text);
        _doc = Sources.BuildDocument(LineCount, DocumentShape.Mixed);
    }

    [Benchmark, BenchmarkCategory("Parse")]
    public async Task<LrcParseResult> ParseAsync_MemoryStream()
    {
        using var ms = new MemoryStream(_bytes, writable: false);
        return await LrcParser.ParseAsync(ms).ConfigureAwait(false);
    }

    [Benchmark, BenchmarkCategory("Parse")]
    public async Task<LrcParseResult> ParseAsync_TextReader()
    {
        using var reader = new StringReader(_text);
        return await LrcParser.ParseAsync(reader).ConfigureAwait(false);
    }

    [Benchmark, BenchmarkCategory("Write")]
    public async Task<long> WriteAsync_MemoryStream()
    {
        using var ms = new MemoryStream(_bytes.Length);
        await LrcWriter.WriteAsync(_doc, ms).ConfigureAwait(false);
        return ms.Length;
    }

    [Benchmark, BenchmarkCategory("Write")]
    public async Task<int> WriteAsync_TextWriter()
    {
        using var sw = new StringWriter();
        await LrcWriter.WriteAsync(_doc, sw).ConfigureAwait(false);
        return sw.GetStringBuilder().Length;
    }
}
