using BenchmarkDotNet.Attributes;
using ModernLrc.Model;

namespace ModernLrc.Benchmarks;

/// <summary>Realistic playback hot-path: <see cref="LrcDocumentExtensions.FindLineAt"/> and
/// <see cref="LrcDocumentExtensions.LinesInRange"/>. A karaoke / lyric-display consumer calls
/// these per video frame, so per-call cost matters more than throughput on a single document.</summary>
[MemoryDiagnoser]
[BenchmarkCategory("Playback")]
public class PlaybackBenchmarks
{
    [Params(100, 1000)]
    public int LineCount { get; set; }

    private LrcDocument _doc = LrcDocument.Empty;
    private TimeSpan[] _queryPoints = Array.Empty<TimeSpan>();

    [GlobalSetup]
    public void Setup()
    {
        _doc = Sources.BuildDocument(LineCount, DocumentShape.Mixed);

        // 16 query points spread across the document, including misses before the first
        // and after the last line — exercises both the binary-search hit and miss paths.
        _queryPoints = new TimeSpan[16];
        long lastMs = LineCount * 1_000L;
        for (int i = 0; i < _queryPoints.Length; i++)
            _queryPoints[i] = TimeSpan.FromMilliseconds((i * lastMs) / (_queryPoints.Length - 1));
    }

    [Benchmark]
    public int FindLineAt_SweepAcrossDocument()
    {
        int hits = 0;
        for (int i = 0; i < _queryPoints.Length; i++)
        {
            if (_doc.FindLineAt(_queryPoints[i]) is not null) hits++;
        }
        return hits;
    }

    [Benchmark]
    public int LinesInRange_HalfDocumentWindow()
    {
        var start = TimeSpan.Zero;
        var end = TimeSpan.FromMilliseconds(LineCount * 500L); // first half
        int count = 0;
        foreach (var _ in _doc.LinesInRange(start, end)) count++;
        return count;
    }

    [Benchmark]
    public TimeSpan GetEffectiveTime_AppliesOffset()
    {
        // Document carries a -150 ms offset (set by Sources.BuildDocument).
        var ts = _doc.Lines[_doc.Lines.Count / 2].Timestamp;
        return _doc.GetEffectiveTime(ts);
    }
}
