using BenchmarkDotNet.Attributes;
using ModernLrc;

namespace ModernLrc.Benchmarks;

/// <summary>Parse-path coverage: every public input shape × document size × document complexity.</summary>
[MemoryDiagnoser]
[CategoriesColumn]
[BenchmarkCategory("Parse")]
public class ParseBenchmarks
{
    [Params(10, 100, 1000)]
    public int LineCount { get; set; }

    [Params(DocumentShape.Simple, DocumentShape.Mixed)]
    public DocumentShape Shape { get; set; }

    private string _text = string.Empty;
    private ReadOnlyMemory<char> _memory;
    private byte[] _bytes = Array.Empty<byte>();

    [GlobalSetup]
    public void Setup()
    {
        _text = Sources.BuildText(LineCount, Shape);
        _memory = _text.AsMemory();
        _bytes = System.Text.Encoding.UTF8.GetBytes(_text);
    }

    [Benchmark(Baseline = true), BenchmarkCategory("Char")]
    public LrcParseResult Parse_String() => LrcParser.Parse(_text);

    [Benchmark, BenchmarkCategory("Char")]
    public LrcParseResult Parse_ReadOnlySpanChar() => LrcParser.Parse(_text.AsSpan());

    [Benchmark, BenchmarkCategory("Char")]
    public LrcParseResult Parse_ReadOnlyMemoryChar() => LrcParser.Parse(_memory);

    [Benchmark, BenchmarkCategory("Char")]
    public LrcParseResult Parse_TextReader()
    {
        using var reader = new StringReader(_text);
        return LrcParser.Parse(reader);
    }

    [Benchmark, BenchmarkCategory("Byte")]
    public LrcParseResult Parse_ReadOnlySpanByte() => LrcParser.Parse((ReadOnlySpan<byte>)_bytes);

    [Benchmark, BenchmarkCategory("Byte")]
    public LrcParseResult Parse_ByteArray() => LrcParser.Parse(_bytes);

    [Benchmark, BenchmarkCategory("Byte")]
    public LrcParseResult Parse_MemoryStream()
    {
        using var ms = new MemoryStream(_bytes, writable: false);
        return LrcParser.Parse(ms);
    }

    [Benchmark, BenchmarkCategory("TryParse")]
    public bool TryParse_String() => LrcParser.TryParse(_text, out _);
}
