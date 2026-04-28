using Opportunity.LrcParser;

namespace ModernLrc.Benchmarks.Comparison.Adapters;

public static class OpportunityLiuAdapter
{
    public static object Parse(string text) => Lyrics.Parse(text);

    public static string Write(object parsed)
    {
        var result = (IParseResult<Line>)parsed;
        return result.Lyrics.ToString();
    }
}
