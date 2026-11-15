using SharpLyrics;

namespace ModernLrc.Benchmarks.Comparison.Adapters;

public static class SharpLyricsAdapter
{
    public static object Parse(string path) => LyricReader.GetLyrics(path);
}
