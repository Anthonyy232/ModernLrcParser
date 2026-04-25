using CsCheck;
using ModernLrc.Model;

namespace ModernLrc.Tests.Generators;

internal static class LrcGen
{
    /// <summary>Generate a non-negative timestamp under 24 hours, quantised to 10 ms
    /// (centisecond resolution) so that the <c>mm:ss.xx</c> format round-trips exactly.</summary>
    public static Gen<LrcTimestamp> Timestamp =>
        Gen.Long[0, 24L * 60 * 60 * 100].Select(cs => LrcTimestamp.FromMilliseconds(cs * 10));

    /// <summary>Plain ASCII text without any LRC structural characters.
    /// Generators exclude <c>[</c>, <c>]</c>, <c>&lt;</c>, <c>&gt;</c>, <c>\r</c>, <c>\n</c>
    /// to keep the round-trip clean.</summary>
    public static Gen<string> SimpleText =>
        Gen.Char[' ', '~']
            .Where(c => c != '[' && c != ']' && c != '<' && c != '>')
            .Array[1, 32]
            .Select(chars => new string(chars));
}
