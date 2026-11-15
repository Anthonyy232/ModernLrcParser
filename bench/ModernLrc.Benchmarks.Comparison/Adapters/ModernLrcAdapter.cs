using ModernLrc;

namespace ModernLrc.Benchmarks.Comparison.Adapters;

public static class ModernLrcAdapter
{
    public static object Parse(string text) => LrcParser.Parse(text);

    public static string Write(object parsed)
    {
        var result = (LrcParseResult)parsed;
        return LrcWriter.Write(result.Document);
    }
}
