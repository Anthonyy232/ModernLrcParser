using System.Diagnostics;

namespace ModernLrc.Model;

/// <summary>Extension helpers for traversing and querying an <see cref="LrcDocument"/>.
/// These are the playback hot path — a karaoke / lyric-display consumer typically calls
/// <see cref="FindLineAt"/> per video frame.</summary>
public static class LrcDocumentExtensions
{
    /// <summary>Apply the document offset to <paramref name="t"/>. Returns <see cref="TimeSpan"/>
    /// (signed) because a large negative <see cref="LrcMetadata.Offset"/> can shift past zero —
    /// <see cref="LrcTimestamp"/> cannot represent that.</summary>
    /// <param name="doc">The owning document.</param>
    /// <param name="t">A timestamp (typically from <see cref="LrcLine.Timestamp"/>).</param>
    /// <returns>The effective time, with offset applied.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="doc"/> is null.</exception>
    /// <example>
    /// <code>
    /// var ts = doc.Lines[0].Timestamp;
    /// TimeSpan effective = doc.GetEffectiveTime(ts);
    /// </code>
    /// </example>
    public static TimeSpan GetEffectiveTime(this LrcDocument doc, LrcTimestamp t)
    {
        ArgumentNullException.ThrowIfNull(doc);
        return t.ToTimeSpan() + doc.Metadata.Offset;
    }

    /// <summary>Currently-singing line: greatest line whose effective timestamp is
    /// ≤ <paramref name="position"/>. O(log n) via binary search.</summary>
    /// <param name="doc">The document to search.</param>
    /// <param name="position">Playhead position (post-offset).</param>
    /// <returns>The matching line, or <c>null</c> if <see cref="LrcDocument.Lines"/> is empty
    /// or <paramref name="position"/> precedes every line. Returns the last line if
    /// <paramref name="position"/> is at or after every line.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="doc"/> is null.</exception>
    /// <example>
    /// <code>
    /// // Per video frame:
    /// var current = doc.FindLineAt(player.Position);
    /// if (current is LrcPlainLine plain) display.Show(plain.Text);
    /// </code>
    /// </example>
    public static LrcLine? FindLineAt(this LrcDocument doc, TimeSpan position)
    {
        ArgumentNullException.ThrowIfNull(doc);
        var lines = doc.Lines;
        if (lines.Count == 0) return null;

        var offset = doc.Metadata.Offset;
        int lo = 0, hi = lines.Count - 1;

        // Binary search for the greatest index whose effective timestamp ≤ position.
        while (lo < hi)
        {
            int mid = lo + ((hi - lo + 1) / 2);
            if (EffectiveAt(lines[mid], offset) <= position) lo = mid;
            else hi = mid - 1;
        }

        if (EffectiveAt(lines[lo], offset) > position) return null;
        return lines[lo];

        static TimeSpan EffectiveAt(LrcLine line, TimeSpan offset)
            => line.Timestamp.ToTimeSpan() + offset;
    }

    /// <summary>Lines whose effective timestamp ∈ <c>[start, end)</c>.
    /// Yielded in document order (sorted by timestamp; ties resolved by original index).</summary>
    /// <param name="doc">The document to scan.</param>
    /// <param name="start">Inclusive lower bound (post-offset).</param>
    /// <param name="end">Exclusive upper bound (post-offset).</param>
    /// <returns>A lazy sequence of matching lines.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="doc"/> is null.</exception>
    public static IEnumerable<LrcLine> LinesInRange(this LrcDocument doc, TimeSpan start, TimeSpan end)
    {
        ArgumentNullException.ThrowIfNull(doc);
        var offset = doc.Metadata.Offset;
        foreach (var line in doc.Lines)
        {
            var t = line.Timestamp.ToTimeSpan() + offset;
            if (t >= start && t < end)
                yield return line;
        }
    }

    /// <summary>Concatenate the textual content of a line. For plain lines: returns
    /// <see cref="LrcPlainLine.Text"/>. For enhanced lines: concatenates every
    /// <see cref="LrcWord.Text"/> in source order (including each word's trailing whitespace,
    /// so the result reproduces the source line exactly).</summary>
    /// <param name="line">The line to flatten.</param>
    /// <returns>The concatenated text.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="line"/> is null.</exception>
    public static string GetText(this LrcLine line)
    {
        ArgumentNullException.ThrowIfNull(line);
#pragma warning disable CS8509 // intentional defence-in-depth arm: hierarchy is sealed today but guard against future subtypes
        return line switch
        {
            LrcPlainLine p => p.Text,
            LrcEnhancedLine e => ConcatWords(e.Words),
            _ => throw new UnreachableException("LrcLine hierarchy is sealed."),
        };
#pragma warning restore CS8509
    }

    private static string ConcatWords(EquatableArray<LrcWord> words)
    {
        if (words.Count == 0) return string.Empty;
        if (words.Count == 1) return words[0].Text;

        int totalLen = 0;
        foreach (var w in words) totalLen += w.Text.Length;

        return string.Create(totalLen, words, static (dest, src) =>
        {
            int pos = 0;
            foreach (var w in src)
            {
                w.Text.AsSpan().CopyTo(dest[pos..]);
                pos += w.Text.Length;
            }
        });
    }
}
