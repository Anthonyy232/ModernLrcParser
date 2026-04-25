using System.Buffers;
using BenchmarkDotNet.Attributes;
using ModernLrc;

namespace ModernLrc.Benchmarks;

/// <summary>Write-path coverage: every public output shape × document size.
/// Document shape fixed to <see cref="DocumentShape.Mixed"/> so all render branches
/// (metadata, voice markers, enhanced word timing, multi-timestamp) are exercised.</summary>
[MemoryDiagnoser]
[CategoriesColumn]
[BenchmarkCategory("Write")]
public class WriteBenchmarks
{
    [Params(10, 100, 1000)]
    public int LineCount { get; set; }

    private LrcDocument _doc = LrcDocument.Empty;
    private int _estimatedSize;
    private char[] _charBuffer = Array.Empty<char>();
    private byte[] _byteBuffer = Array.Empty<byte>();

    [GlobalSetup]
    public void Setup()
    {
        _doc = Sources.BuildDocument(LineCount, DocumentShape.Mixed);
        _estimatedSize = LrcWriter.EstimateSize(_doc);
        // TryWrite buffers — sized generously so the success path is measured.
        _charBuffer = new char[_estimatedSize * 2];
        _byteBuffer = new byte[LrcWriter.EstimateByteSize(_doc) + 64];
    }

    [Benchmark(Baseline = true), BenchmarkCategory("Char")]
    public string Write_String() => LrcWriter.Write(_doc);

    [Benchmark, BenchmarkCategory("Char")]
    public int Write_TextWriter()
    {
        using var sw = new StringWriter();
        LrcWriter.Write(_doc, sw);
        return sw.GetStringBuilder().Length;
    }

    [Benchmark, BenchmarkCategory("Char")]
    public int Write_IBufferWriterChar()
    {
        var bw = new ArrayBufferWriter<char>(_estimatedSize);
        LrcWriter.Write(_doc, bw);
        return bw.WrittenCount;
    }

    [Benchmark, BenchmarkCategory("Char")]
    public bool TryWrite_Char() => LrcWriter.TryWrite(_doc, _charBuffer, out _);

    [Benchmark, BenchmarkCategory("Byte")]
    public int Write_IBufferWriterBytes()
    {
        var bw = new ArrayBufferWriter<byte>(_estimatedSize);
        LrcWriter.Write(_doc, bw);
        return bw.WrittenCount;
    }

    [Benchmark, BenchmarkCategory("Byte")]
    public long Write_Stream()
    {
        using var ms = new MemoryStream(_estimatedSize);
        LrcWriter.Write(_doc, ms);
        return ms.Length;
    }

    [Benchmark, BenchmarkCategory("Byte")]
    public bool TryWrite_Byte() => LrcWriter.TryWrite(_doc, _byteBuffer, out _);

    [Benchmark, BenchmarkCategory("Estimate")]
    public int EstimateSize() => LrcWriter.EstimateSize(_doc);
}
