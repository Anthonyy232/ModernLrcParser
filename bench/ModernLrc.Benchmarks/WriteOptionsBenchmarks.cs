using System.Buffers;
using System.Text;
using BenchmarkDotNet.Attributes;
using ModernLrc;
using ModernLrc.Model;

namespace ModernLrc.Benchmarks;

/// <summary>Tracks per-render cost of write-option variants that hit different code paths:
/// <list type="bullet">
///   <item>Alphabetical metadata ordering — exercises the <c>RentSortedNonTypedTags</c> ArrayPool sort path.</item>
///   <item><see cref="LrcWriteOptions.CollapseIdenticalLines"/> — exercises the iterator-based collapse path.</item>
///   <item><see cref="LrcWriteOptions.EmitByteOrderMark"/> — adds preamble emission to the byte path.</item>
/// </list>
/// All paths render to in-memory buffers; no I/O.</summary>
[MemoryDiagnoser]
[CategoriesColumn]
[BenchmarkCategory("WriteOptions")]
public class WriteOptionsBenchmarks
{
    private const int LineCount = 200;

    private LrcDocument _doc = LrcDocument.Empty;
    private LrcDocument _docWithRepeats = LrcDocument.Empty;

    private LrcWriteOptions _default = LrcWriteOptions.Default;
    private LrcWriteOptions _alphabetical = LrcWriteOptions.Default;
    private LrcWriteOptions _collapse = LrcWriteOptions.Default;
    private LrcWriteOptions _bom = LrcWriteOptions.Default;

    [GlobalSetup]
    public void Setup()
    {
        // Document with several non-strongly-typed RawTags so the alphabetical sort
        // path actually has work to do (otherwise the count==0 fast path runs).
        var baseDoc = Sources.BuildDocument(LineCount, DocumentShape.Mixed);
        _doc = baseDoc with
        {
            Metadata = baseDoc.Metadata with
            {
                RawTags = System.Collections.Immutable.ImmutableArray.Create(
                    new LrcTag("zzz", "z-value"),
                    new LrcTag("aaa", "a-value"),
                    new LrcTag("mmm", "m-value"),
                    new LrcTag("kkk", "k-value"),
                    new LrcTag("bbb", "b-value")),
            },
        };

        // Document where many adjacent lines share text — exercises CollapseIdenticalLines.
        var builder = new LrcDocumentBuilder().WithTitle("Repeats");
        for (int i = 0; i < LineCount; i++)
            builder.AddLine(LrcTimestamp.FromMilliseconds(i * 1_000L), "chorus");
        _docWithRepeats = builder.Build();

        _default = LrcWriteOptions.Default;
        _alphabetical = LrcWriteOptions.Default with { MetadataOrdering = LrcMetadataOrdering.Alphabetical };
        _collapse = LrcWriteOptions.Default with { CollapseIdenticalLines = true };
        _bom = LrcWriteOptions.Default with { EmitByteOrderMark = true, Encoding = Encoding.UTF8 };
    }

    [Benchmark(Baseline = true), BenchmarkCategory("Char")]
    public string Write_Default() => LrcWriter.Write(_doc, _default);

    [Benchmark, BenchmarkCategory("Char")]
    public string Write_Alphabetical() => LrcWriter.Write(_doc, _alphabetical);

    [Benchmark, BenchmarkCategory("Char")]
    public string Write_CollapseIdentical() => LrcWriter.Write(_docWithRepeats, _collapse);

    [Benchmark, BenchmarkCategory("Byte")]
    public int Write_IBufferWriterByte_Default()
    {
        var bw = new ArrayBufferWriter<byte>(LineCount * 32);
        LrcWriter.Write(_doc, bw, _default);
        return bw.WrittenCount;
    }

    [Benchmark, BenchmarkCategory("Byte")]
    public int Write_IBufferWriterByte_Alphabetical()
    {
        var bw = new ArrayBufferWriter<byte>(LineCount * 32);
        LrcWriter.Write(_doc, bw, _alphabetical);
        return bw.WrittenCount;
    }

    [Benchmark, BenchmarkCategory("Byte")]
    public long Write_Stream_WithBom()
    {
        using var ms = new MemoryStream(LineCount * 32);
        LrcWriter.Write(_doc, ms, _bom);
        return ms.Length;
    }
}
