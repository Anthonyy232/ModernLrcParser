using KaraokeLrcParser = LrcParser.Parser.Lrc.LrcParser;
using LrcParser.Model;

namespace ModernLrc.Benchmarks.Comparison.Adapters;

public static class KaraokeDevAdapter
{
    private static readonly KaraokeLrcParser Parser = new();

    public static object Parse(string text) => Parser.Decode(text);

    public static string Write(object parsed) => Parser.Encode((Song)parsed);
}
