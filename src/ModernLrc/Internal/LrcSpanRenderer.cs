using System.Buffers;
using ModernLrc.Model;
using F = ModernLrc.Model.LrcMetadataField;

namespace ModernLrc.Internal;

/// <summary>
/// Single render path used by every <see cref="LrcWriter"/> sink. Writes an
/// <see cref="LrcDocument"/> into an <see cref="IBufferWriter{T}"/> of chars; byte sinks
/// encode the result with <see cref="LrcWriteOptions.Encoding"/>.
/// </summary>
internal static class LrcSpanRenderer
{
    private static readonly (string Key, F Field)[] CanonicalOrder =
    [
        ("ti", F.Title), ("ar", F.Artist), ("al", F.Album), ("au", F.Author), ("lr", F.Lyricist),
        ("length", F.Length), ("by", F.CreatedBy), ("offset", F.Offset), ("re", F.Tool), ("ve", F.Version),
    ];

    private static readonly (string Key, F Field)[] AlphabeticalOrder =
    [
        ("al", F.Album), ("ar", F.Artist), ("au", F.Author), ("by", F.CreatedBy), ("length", F.Length),
        ("lr", F.Lyricist), ("offset", F.Offset), ("ti", F.Title), ("re", F.Tool), ("ve", F.Version),
    ];

    public static void Render(LrcDocument document, IBufferWriter<char> writer, LrcWriteOptions options)
    {
        string lineEnding = ResolveLineEnding(options.LineEnding);
        var w = new CharBufferWriter(writer);

        // Line endings go BETWEEN items; the trailing one is emitted once at the end,
        // conditional on TrailingNewline.
        bool metaWritten = RenderMetadata(document.Metadata, ref w, options, lineEnding);
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

    private static bool RenderMetadata(LrcMetadata metadata, ref CharBufferWriter w,
        LrcWriteOptions options, string lineEnding)
    {
        // Raw tags of a known key are emitted verbatim (preserving duplicates and source order)
        // only while re-parsing them still yields the typed property. Once the typed property
        // has been edited, its raw tags are stale: they are dropped and the typed value wins.
        Span<bool> rawAgrees = stackalloc bool[LrcMetadataFields.Count];
        foreach (var (_, field) in CanonicalOrder)
            rawAgrees[(int)field] = LrcMetadataFields.RawTagsAgree(metadata, field);

        bool any = false;
        var order = options.MetadataOrdering == LrcMetadataOrdering.Canonical ? CanonicalOrder : AlphabeticalOrder;
        foreach (var (key, field) in order)
        {
            // With no raw tags, "agreeing" means the typed value is unset, so nothing is emitted.
            if (!rawAgrees[(int)field])
                AppendTag(ref w, key, LrcMetadataFields.Format(metadata, field), lineEnding, ref any);
        }

        IEnumerable<LrcTag> rawTags = metadata.RawTags;
        if (options.MetadataOrdering == LrcMetadataOrdering.Alphabetical)
            rawTags = rawTags.OrderBy(static t => t.Key, StringComparer.Ordinal);
        foreach (var tag in rawTags)
        {
            if (LrcMetadataFields.FromKey(tag.Key) is { } field && !rawAgrees[(int)field]) continue;
            AppendTag(ref w, tag.Key, tag.Value, lineEnding, ref any);
        }

        return any;
    }

    private static void AppendTag(ref CharBufferWriter w, string key, string? value, string lineEnding, ref bool any)
    {
        if (value is null) return;
        if (any) w.Append(lineEnding);
        w.Append('['); w.Append(key); w.Append(':'); w.Append(value); w.Append(']');
        any = true;
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
