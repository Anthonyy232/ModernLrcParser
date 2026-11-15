using Kfstorm.LrcParser;

namespace ModernLrc.Benchmarks.Comparison.Adapters;

public static class KfstormAdapter
{
    public static object Parse(string text) => LrcFile.FromText(text);
}
