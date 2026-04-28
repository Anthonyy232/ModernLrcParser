using System.Globalization;
using System.Text;

namespace ModernLrc.Benchmarks.Comparison;

public sealed record NamedInput(string Name, string Text)
{
    public override string ToString() => Name;
}

public static class Corpus
{
    private const string Header =
        "[ti:Sample Title]\n" +
        "[ar:Sample Artist]\n" +
        "[al:Sample Album]\n" +
        "[by:Benchmark Suite]\n" +
        "[offset:0]\n";

    public static readonly NamedInput Small = new("Small", BuildBasic(20));
    public static readonly NamedInput Medium = new("Medium", BuildBasic(200));
    public static readonly NamedInput Large = new("Large", BuildBasic(2000));

    public static IEnumerable<NamedInput> All => [Small, Medium, Large];

    private static string BuildBasic(int lineCount)
    {
        var sb = new StringBuilder(Header.Length + lineCount * 32);
        sb.Append(Header);
        const int stepMs = 250;
        for (var i = 0; i < lineCount; i++)
        {
            var totalMs = i * stepMs;
            var minutes = totalMs / 60_000;
            var seconds = totalMs / 1000 % 60;
            var hundredths = totalMs % 1000 / 10;
            sb.Append('[');
            sb.Append(minutes.ToString("D2", CultureInfo.InvariantCulture));
            sb.Append(':');
            sb.Append(seconds.ToString("D2", CultureInfo.InvariantCulture));
            sb.Append('.');
            sb.Append(hundredths.ToString("D2", CultureInfo.InvariantCulture));
            sb.Append(']');
            sb.Append("Line number ");
            sb.Append(i.ToString(CultureInfo.InvariantCulture));
            sb.Append(" with some lyric content");
            sb.Append('\n');
        }
        return sb.ToString();
    }
}
