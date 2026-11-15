using System.Buffers;
using ModernLrc.Model;

namespace ModernLrc.Internal;

/// <summary>
/// Single render path used by every <see cref="LrcWriter"/> sink. Writes an
/// <see cref="LrcDocument"/> into an <see cref="IBufferWriter{T}"/> of chars; byte sinks
/// encode the result with <see cref="LrcWriteOptions.Encoding"/>.
/// </summary>
internal static class LrcSpanRenderer
{
    public static void Render(LrcDocument document, IBufferWriter<char> writer, LrcWriteOptions options)
    {
        string lineEnding = ResolveLineEnding(options.LineEnding);
        var w = new CharBufferWriter(writer);

        // Line endings go BETWEEN items; the trailing one is emitted once at the end,
        // conditional on TrailingNewline.
        bool metaWritten = false;
        foreach (var tag in LrcMetadataFields.WrittenTags(document.Metadata, options.MetadataOrdering))
        {
            if (metaWritten) w.Append(lineEnding);
            w.Append('['); w.Append(tag.Key); w.Append(':'); w.Append(tag.Value); w.Append(']');
            metaWritten = true;
        }

        bool linesWritten = document.Lines.Count > 0;

        if (metaWritten && linesWritten)
        {
            // Terminate the final metadata tag, then a blank line before lyrics.
            w.Append(lineEnding);
            w.Append(lineEnding);
        }

        if (linesWritten)
            RenderLyrics(document, ref w, options, lineEnding);

        if ((metaWritten || linesWritten) && options.TrailingNewline)
            w.Append(lineEnding);

        w.Commit();
    }

    private static void RenderLyrics(LrcDocument document, ref CharBufferWriter w,
        LrcWriteOptions options, string lineEnding)
    {
        var lastVoice = LrcVoice.Default;
        ReadOnlySpan<char> tsFormat = options.TimestampPrecision == LrcTimestampPrecision.Milliseconds
            ? "F" : "G";

        var lines = document.Lines.AsSpan();

        // With CollapseIdenticalLines, consecutive same-content lines are emitted as one
        // [t1][t2]text group — the inverse of the parser's fan-out.
        int i = 0;
        while (i < lines.Length)
        {
            int j = i + 1;
            while (options.CollapseIdenticalLines && j < lines.Length && CanCollapse(lines[i], lines[j])) j++;
            if (i > 0) w.Append(lineEnding);
            for (int k = i; k < j; k++)
            {
                w.Append('[');
                w.AppendInvariant(lines[k].Timestamp, tsFormat);
                w.Append(']');
            }
            AppendVoiceMarker(ref w, lines[i], options, ref lastVoice);
            AppendBody(ref w, lines[i], tsFormat);
            i = j;
        }
    }

    private static void AppendVoiceMarker(ref CharBufferWriter w, LrcLine line,
        LrcWriteOptions options, ref LrcVoice lastVoice)
    {
        if (!options.EmitVoiceMarkers || line.EffectiveVoice == LrcVoice.Default) return;
        if (options.VoiceMarkerOnChangeOnly && line.EffectiveVoice == lastVoice) return;
        char marker = line.EffectiveVoice switch
        {
            LrcVoice.Male => 'M',
            LrcVoice.Female => 'F',
            LrcVoice.Duet => 'D',
            _ => '\0',
        };
        if (marker == '\0') return;
        w.Append(marker); w.Append(':'); w.Append(' ');
        lastVoice = line.EffectiveVoice;
    }

    private static void AppendBody(ref CharBufferWriter w, LrcLine line, ReadOnlySpan<char> tsFormat)
    {
        switch (line)
        {
            case LrcPlainLine plain:
                w.Append(plain.Text);
                break;
            case LrcEnhancedLine enhanced:
                foreach (var word in enhanced.Words)
                {
                    w.Append('<');
                    w.AppendInvariant(word.Timestamp, tsFormat);
                    w.Append('>');
                    w.Append(word.Text);
                }
                break;
        }
    }

    internal static string ResolveLineEnding(LrcLineEnding ending) => ending switch
    {
        LrcLineEnding.Crlf => "\r\n",
        LrcLineEnding.Cr => "\r",
        LrcLineEnding.System when Environment.NewLine is "\r\n" or "\r" => Environment.NewLine,
        _ => "\n",
    };

    /// <summary>True when two consecutive lines carry identical content and voice — i.e. the
    /// input was a <c>[t1][t2]text</c> group that the parser fanned out into separate lines.</summary>
    private static bool CanCollapse(LrcLine a, LrcLine b)
        => a.EffectiveVoice == b.EffectiveVoice && (a, b) switch
        {
            (LrcPlainLine pa, LrcPlainLine pb) => string.Equals(pa.Text, pb.Text, StringComparison.Ordinal),
            (LrcEnhancedLine ea, LrcEnhancedLine eb) => ea.Words == eb.Words,
            _ => false,
        };
}
