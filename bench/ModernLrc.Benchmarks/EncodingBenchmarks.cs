using System.Text;
using BenchmarkDotNet.Attributes;
using ModernLrc;

namespace ModernLrc.Benchmarks;

/// <summary>Encoding pipeline cost: BOM detection vs forced encoding vs UTF-8 fast path.
/// Same logical content (100 mixed-shape lines), different byte representations.</summary>
[MemoryDiagnoser]
[BenchmarkCategory("Encoding")]
public class EncodingBenchmarks
{
    private const int LineCount = 100;
    private const DocumentShape Shape = DocumentShape.Mixed;

    private byte[] _utf8NoBom = Array.Empty<byte>();
    private byte[] _utf8Bom = Array.Empty<byte>();
    private byte[] _utf16Le = Array.Empty<byte>();
    private byte[] _utf16Be = Array.Empty<byte>();

    [GlobalSetup]
    public void Setup()
    {
        _utf8NoBom = Sources.BuildBytes(LineCount, Shape, new UTF8Encoding(false), bom: false);
        _utf8Bom   = Sources.BuildBytes(LineCount, Shape, new UTF8Encoding(true),  bom: true);
        _utf16Le   = Sources.BuildBytes(LineCount, Shape, Encoding.Unicode,        bom: true);
        _utf16Be   = Sources.BuildBytes(LineCount, Shape, Encoding.BigEndianUnicode, bom: true);
    }

    [Benchmark(Baseline = true)]
    public LrcParseResult Bytes_Utf8_NoBom() => LrcParser.Parse(_utf8NoBom);

    [Benchmark]
    public LrcParseResult Bytes_Utf8_WithBom() => LrcParser.Parse(_utf8Bom);

    [Benchmark]
    public LrcParseResult Bytes_Utf16Le_Bom() => LrcParser.Parse(_utf16Le);

    [Benchmark]
    public LrcParseResult Bytes_Utf16Be_Bom() => LrcParser.Parse(_utf16Be);
}
