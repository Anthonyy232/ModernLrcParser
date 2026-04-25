using System.Globalization;
using System.Text;
using ModernLrc;
using ModernLrc.Model;

namespace ModernLrc.Benchmarks;

public enum DocumentShape
{
    /// <summary>Plain <c>[mm:ss.xx]text</c> lines only — minimal feature set.</summary>
    Simple,

    /// <summary>Full feature set: metadata block, offset, voice markers (F/M),
    /// multi-timestamp lines, and Enhanced LRC word timing every Nth line.</summary>
    Mixed,
}

internal static class Sources
{
    public static LrcDocument BuildDocument(int lineCount, DocumentShape shape)
    {
        var builder = new LrcDocumentBuilder()
            .WithTitle("Demo Song")
            .WithArtist("Tester")
            .WithAlbum("Demos")
            .WithAuthor("ModernLrc Benchmarks")
            .WithOffset(TimeSpan.FromMilliseconds(-150));

        if (shape == DocumentShape.Simple)
        {
            for (int i = 0; i < lineCount; i++)
                builder.AddLine(LrcTimestamp.FromMilliseconds(i * 1_000L), $"line {i} simple lyric content");
            return builder.Build();
        }

        // Mixed shape distribution:
        //   - voice rotates Default → Female → Male every line (exercises voice tracker)
        //   - every 5th line uses Enhanced LRC word timing (exercises <...> render path)
        //   - every 7th line uses two timestamps for the same text (exercises multi-timestamp)
        for (int i = 0; i < lineCount; i++)
        {
            long t = i * 1_000L;
            var ts = LrcTimestamp.FromMilliseconds(t);
            var voice = (i % 3) switch
            {
                0 => LrcVoice.Default,
                1 => LrcVoice.Female,
                _ => LrcVoice.Male,
            };

            if (i % 5 == 4)
            {
                var words = new[]
                {
                    new LrcWord(LrcTimestamp.FromMilliseconds(t),       "word "),
                    new LrcWord(LrcTimestamp.FromMilliseconds(t + 200), "by "),
                    new LrcWord(LrcTimestamp.FromMilliseconds(t + 400), "word "),
                    new LrcWord(LrcTimestamp.FromMilliseconds(t + 600), "enhanced"),
                };
                builder.AddEnhancedLine(ts, words, voice);
            }
            else if (i % 7 == 6)
            {
                var stamps = new[]
                {
                    LrcTimestamp.FromMilliseconds(t),
                    LrcTimestamp.FromMilliseconds(t + 60_000),
                };
                builder.AddLine(stamps, $"chorus line {i} repeats", voice);
            }
            else
            {
                builder.AddLine(ts, $"line {i} mixed lyric text content", voice);
            }
        }

        return builder.Build();
    }

    public static string BuildText(int lineCount, DocumentShape shape)
        => LrcWriter.Write(BuildDocument(lineCount, shape));

    public static byte[] BuildBytes(int lineCount, DocumentShape shape, Encoding encoding, bool bom)
    {
        string text = BuildText(lineCount, shape);
        byte[] preamble = bom ? encoding.GetPreamble() : Array.Empty<byte>();
        byte[] body = encoding.GetBytes(text);
        if (preamble.Length == 0) return body;
        var result = new byte[preamble.Length + body.Length];
        Buffer.BlockCopy(preamble, 0, result, 0, preamble.Length);
        Buffer.BlockCopy(body, 0, result, preamble.Length, body.Length);
        return result;
    }

    /// <summary>Build text with N% of lines deliberately malformed — exercises the
    /// recovery / diagnostic emission path.</summary>
    public static string BuildErrorRichText(int lineCount, int badLineEveryNth)
    {
        var sb = new StringBuilder(lineCount * 32);
        sb.AppendLine("[ti:Error Demo]");
        sb.AppendLine("[ar:Tester]");
        sb.AppendLine();
        for (int i = 0; i < lineCount; i++)
        {
            var inv = CultureInfo.InvariantCulture;
            if (i % badLineEveryNth == 0)
            {
                // Mix of recoverable variants: missing bracket, bad fraction, junk prefix.
                _ = (i % 3) switch
                {
                    0 => sb.Append(inv, $"[00:{i % 60:D2}.99x]bad fraction line {i}\n"),
                    1 => sb.Append(inv, $"00:{i % 60:D2}.50]missing bracket line {i}\n"),
                    _ => sb.Append(inv, $"junk-prefix [00:{i % 60:D2}.50]line {i}\n"),
                };
            }
            else
            {
                sb.Append(inv, $"[{i / 60:D2}:{i % 60:D2}.50]line {i}\n");
            }
        }
        return sb.ToString();
    }
}
