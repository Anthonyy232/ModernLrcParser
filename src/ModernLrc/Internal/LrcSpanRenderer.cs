using System.Buffers;
using ModernLrc.Model;

namespace ModernLrc.Internal;

/// <summary>
/// Single render path used by every <see cref="LrcWriter"/> sink. Writes an
/// <see cref="LrcDocument"/> directly into an <see cref="IBufferWriter{T}"/> of chars or
/// UTF-8 bytes — no intermediate string allocation. The <see cref="LrcWriter.Write(LrcDocument, LrcWriteOptions?)"/>
/// (string) and <see cref="LrcWriter.Write(LrcDocument, TextWriter, LrcWriteOptions?)"/>
/// overloads stage into an <see cref="System.Buffers.ArrayBufferWriter{T}"/> of chars and
/// dispatch to <see cref="RenderToChars"/>, so all sink shapes share this code.
/// </summary>
internal static class LrcSpanRenderer
{
    // -------------------------------------------------------------------------
    // Char path
    // -------------------------------------------------------------------------

    /// <summary>
    /// Render <paramref name="document"/> directly into <paramref name="writer"/> as characters.
    /// 0 heap allocations beyond consumer buffer growth for valid input.
    /// </summary>
    public static void RenderToChars(LrcDocument document, IBufferWriter<char> writer, LrcWriteOptions options)
    {
        string lineEnding = ResolveLineEnding(options.LineEnding);
        var w = new CharBufferWriter(writer, options.InitialBufferSize);

        // Emit pattern: each item writes content WITHOUT a trailing line ending; line endings
        // go BETWEEN items. The trailing line ending is then emitted exactly once, conditional
        // on TrailingNewline. This avoids any need to walk back and trim a trailing \r/\n that
        // may have been flushed past the unwritable boundary by a buffer rotation.
        bool metaWritten = RenderMetadataChars(document.Metadata, ref w, options, lineEnding);
        bool linesWritten = document.Lines.Count > 0;

        if (metaWritten && linesWritten)
        {
            // Terminate the final metadata tag, then a blank line before lyrics.
            w.Append(lineEnding);
            w.Append(lineEnding);
        }

        if (linesWritten)
            RenderLyricsChars(document, ref w, options, lineEnding);

        if ((metaWritten || linesWritten) && options.TrailingNewline)
            w.Append(lineEnding);

        w.Commit();
    }

    private static bool RenderMetadataChars(LrcMetadata metadata, ref CharBufferWriter w,
        LrcWriteOptions options, string lineEnding)
    {
        // any tracks whether anything was written; doubles as the "needs leading line ending"
        // flag so each subsequent item writes its separator BEFORE its content.
        bool any = false;

        if (options.MetadataOrdering == LrcMetadataOrdering.Canonical)
        {
            any |= AppendTagIfChars(ref w, "ti", metadata.Title, lineEnding, any);
            any |= AppendTagIfChars(ref w, "ar", metadata.Artist, lineEnding, any);
            any |= AppendTagIfChars(ref w, "al", metadata.Album, lineEnding, any);
            any |= AppendTagIfChars(ref w, "au", metadata.Author, lineEnding, any);
            any |= AppendTagIfChars(ref w, "lr", metadata.Lyricist, lineEnding, any);
            if (metadata.Length is not null)
            {
                AppendLengthChars(ref w, metadata.Length.Value, lineEnding, any);
                any = true;
            }
            any |= AppendTagIfChars(ref w, "by", metadata.CreatedBy, lineEnding, any);
            if (metadata.Offset != TimeSpan.Zero)
            {
                AppendOffsetChars(ref w, metadata.Offset, lineEnding, any);
                any = true;
            }
            any |= AppendTagIfChars(ref w, "re", metadata.Tool, lineEnding, any);
            any |= AppendTagIfChars(ref w, "ve", metadata.Version, lineEnding, any);

            foreach (var tag in metadata.RawTags)
            {
                if (IsStronglyTypedKey(tag.Key)) continue;
                if (any) w.Append(lineEnding);
                w.Append('['); w.Append(tag.Key); w.Append(':'); w.Append(tag.Value); w.Append(']');
                any = true;
            }
        }
        else // Alphabetical
        {
            any |= AppendTagIfChars(ref w, "al", metadata.Album, lineEnding, any);
            any |= AppendTagIfChars(ref w, "ar", metadata.Artist, lineEnding, any);
            any |= AppendTagIfChars(ref w, "au", metadata.Author, lineEnding, any);
            any |= AppendTagIfChars(ref w, "by", metadata.CreatedBy, lineEnding, any);
            if (metadata.Length is not null)
            {
                AppendLengthChars(ref w, metadata.Length.Value, lineEnding, any);
                any = true;
            }
            any |= AppendTagIfChars(ref w, "lr", metadata.Lyricist, lineEnding, any);
            if (metadata.Offset != TimeSpan.Zero)
            {
                AppendOffsetChars(ref w, metadata.Offset, lineEnding, any);
                any = true;
            }
            any |= AppendTagIfChars(ref w, "ti", metadata.Title, lineEnding, any);
            any |= AppendTagIfChars(ref w, "re", metadata.Tool, lineEnding, any);
            any |= AppendTagIfChars(ref w, "ve", metadata.Version, lineEnding, any);

            // RawTags A→Z by key (NOT typed-key duplicates).
            int rawCount = RentSortedNonTypedTags(metadata, out var rented);
            try
            {
                for (int i = 0; i < rawCount; i++)
                {
                    var tag = rented[i];
                    if (any) w.Append(lineEnding);
                    w.Append('['); w.Append(tag.Key); w.Append(':'); w.Append(tag.Value); w.Append(']');
                    any = true;
                }
            }
            finally
            {
                if (rawCount > 0) ArrayPool<LrcTag>.Shared.Return(rented, clearArray: true);
            }
        }

        return any;
    }

    private static bool AppendTagIfChars(ref CharBufferWriter w, string key, string? value, string lineEnding, bool needsLeadingNewline)
    {
        if (value is null) return false;
        if (needsLeadingNewline) w.Append(lineEnding);
        w.Append('['); w.Append(key); w.Append(':'); w.Append(value); w.Append(']');
        return true;
    }

    private static void AppendLengthChars(ref CharBufferWriter w, TimeSpan length, string lineEnding, bool needsLeadingNewline)
    {
        long mm = (long)length.TotalMinutes;
        int ss = length.Seconds;
        if (needsLeadingNewline) w.Append(lineEnding);
        w.Append("[length:");
        w.AppendInvariant(mm, "D2");
        w.Append(':');
        w.AppendInvariant(ss, "D2");
        w.Append(']');
    }

    private static void AppendOffsetChars(ref CharBufferWriter w, TimeSpan offset, string lineEnding, bool needsLeadingNewline)
    {
        long ms = (long)offset.TotalMilliseconds;
        if (needsLeadingNewline) w.Append(lineEnding);
        w.Append("[offset:");
        if (ms >= 0) w.Append('+');
        w.AppendInvariant(ms);
        w.Append(']');
    }

    private static void RenderLyricsChars(LrcDocument document, ref CharBufferWriter w,
        LrcWriteOptions options, string lineEnding)
    {
        var lastVoice = LrcVoice.Default;
        ReadOnlySpan<char> tsFormat = options.TimestampPrecision == LrcTimestampPrecision.Milliseconds
            ? "F" : "G";

        // Two iteration paths:
        // - Non-collapse: span enumeration over the underlying ImmutableArray<LrcLine> avoids
        //   the boxed IEnumerator<LrcLine> that an IEnumerable cast would allocate.
        // - Collapse: must iterate via IEnumerable to use the existing CollapseIdentical state
        //   machine. The boxing cost is unavoidable on this path.
        if (!options.CollapseIdenticalLines)
        {
            bool first = true;
            foreach (var line in document.Lines.AsSpan())
            {
                if (!first) w.Append(lineEnding);
                first = false;
                RenderLineChars(line, ref w, options, tsFormat, ref lastVoice);
            }
        }
        else
        {
            bool first = true;
            foreach (var line in CollapseIdentical(document.Lines))
            {
                if (!first) w.Append(lineEnding);
                first = false;
                RenderLineChars(line, ref w, options, tsFormat, ref lastVoice);
            }
        }
    }

    private static void RenderLineChars(LrcLine line, ref CharBufferWriter w,
        LrcWriteOptions options, ReadOnlySpan<char> tsFormat, ref LrcVoice lastVoice)
    {
        // Timestamps
        foreach (var ts in line.Timestamps)
        {
            w.Append('[');
            w.AppendInvariant(ts, tsFormat);
            w.Append(']');
        }

        // Voice marker
        if (options.EmitVoiceMarkers && line.EffectiveVoice != LrcVoice.Default)
        {
            if (!options.VoiceMarkerOnChangeOnly || line.EffectiveVoice != lastVoice)
            {
                char marker = line.EffectiveVoice switch
                {
                    LrcVoice.Male => 'M',
                    LrcVoice.Female => 'F',
                    LrcVoice.Duet => 'D',
                    _ => '\0',
                };
                if (marker != '\0')
                {
                    w.Append(marker); w.Append(':'); w.Append(' ');
                    lastVoice = line.EffectiveVoice;
                }
            }
        }

        // Content
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

    // -------------------------------------------------------------------------
    // UTF-8 byte path
    // -------------------------------------------------------------------------

    /// <summary>
    /// Render <paramref name="document"/> directly into <paramref name="writer"/> as UTF-8 bytes.
    /// 0 heap allocations beyond consumer buffer growth for valid Unicode input.
    /// </summary>
    public static void RenderToUtf8(LrcDocument document, IBufferWriter<byte> writer, LrcWriteOptions options)
    {
        byte[] lineEndingBytes = ResolveLineEndingBytes(options.LineEnding);
        var w = new Utf8BufferWriter(writer, options.InitialBufferSize);

        // Mirror the chars path: line endings BETWEEN items, then a single conditional trailing
        // line ending. Eliminates the rotation hazard that an after-the-fact trim would incur.
        bool metaWritten = RenderMetadataUtf8(document.Metadata, ref w, options, lineEndingBytes);
        bool linesWritten = document.Lines.Count > 0;

        if (metaWritten && linesWritten)
        {
            w.Append(lineEndingBytes);
            w.Append(lineEndingBytes);
        }

        if (linesWritten)
            RenderLyricsUtf8(document, ref w, options, lineEndingBytes);

        if ((metaWritten || linesWritten) && options.TrailingNewline)
            w.Append(lineEndingBytes);

        w.Commit();
    }

    private static bool RenderMetadataUtf8(LrcMetadata metadata, ref Utf8BufferWriter w,
        LrcWriteOptions options, byte[] lineEnding)
    {
        bool any = false;

        if (options.MetadataOrdering == LrcMetadataOrdering.Canonical)
        {
            any |= AppendTagIfUtf8(ref w, "ti", metadata.Title, lineEnding, any);
            any |= AppendTagIfUtf8(ref w, "ar", metadata.Artist, lineEnding, any);
            any |= AppendTagIfUtf8(ref w, "al", metadata.Album, lineEnding, any);
            any |= AppendTagIfUtf8(ref w, "au", metadata.Author, lineEnding, any);
            any |= AppendTagIfUtf8(ref w, "lr", metadata.Lyricist, lineEnding, any);
            if (metadata.Length is not null)
            {
                AppendLengthUtf8(ref w, metadata.Length.Value, lineEnding, any);
                any = true;
            }
            any |= AppendTagIfUtf8(ref w, "by", metadata.CreatedBy, lineEnding, any);
            if (metadata.Offset != TimeSpan.Zero)
            {
                AppendOffsetUtf8(ref w, metadata.Offset, lineEnding, any);
                any = true;
            }
            any |= AppendTagIfUtf8(ref w, "re", metadata.Tool, lineEnding, any);
            any |= AppendTagIfUtf8(ref w, "ve", metadata.Version, lineEnding, any);

            foreach (var tag in metadata.RawTags)
            {
                if (IsStronglyTypedKey(tag.Key)) continue;
                if (any) w.Append(lineEnding);
                w.Append("["u8); w.AppendText(tag.Key); w.Append(":"u8); w.AppendText(tag.Value); w.Append("]"u8);
                any = true;
            }
        }
        else // Alphabetical
        {
            any |= AppendTagIfUtf8(ref w, "al", metadata.Album, lineEnding, any);
            any |= AppendTagIfUtf8(ref w, "ar", metadata.Artist, lineEnding, any);
            any |= AppendTagIfUtf8(ref w, "au", metadata.Author, lineEnding, any);
            any |= AppendTagIfUtf8(ref w, "by", metadata.CreatedBy, lineEnding, any);
            if (metadata.Length is not null)
            {
                AppendLengthUtf8(ref w, metadata.Length.Value, lineEnding, any);
                any = true;
            }
            any |= AppendTagIfUtf8(ref w, "lr", metadata.Lyricist, lineEnding, any);
            if (metadata.Offset != TimeSpan.Zero)
            {
                AppendOffsetUtf8(ref w, metadata.Offset, lineEnding, any);
                any = true;
            }
            any |= AppendTagIfUtf8(ref w, "ti", metadata.Title, lineEnding, any);
            any |= AppendTagIfUtf8(ref w, "re", metadata.Tool, lineEnding, any);
            any |= AppendTagIfUtf8(ref w, "ve", metadata.Version, lineEnding, any);

            int rawCount = RentSortedNonTypedTags(metadata, out var rented);
            try
            {
                for (int i = 0; i < rawCount; i++)
                {
                    var tag = rented[i];
                    if (any) w.Append(lineEnding);
                    w.Append("["u8); w.AppendText(tag.Key); w.Append(":"u8); w.AppendText(tag.Value); w.Append("]"u8);
                    any = true;
                }
            }
            finally
            {
                if (rawCount > 0) ArrayPool<LrcTag>.Shared.Return(rented, clearArray: true);
            }
        }

        return any;
    }

    private static bool AppendTagIfUtf8(ref Utf8BufferWriter w, string key, string? value, byte[] lineEnding, bool needsLeadingNewline)
    {
        if (value is null) return false;
        if (needsLeadingNewline) w.Append(lineEnding);
        w.Append("["u8); w.AppendText(key); w.Append(":"u8); w.AppendText(value); w.Append("]"u8);
        return true;
    }

    private static void AppendLengthUtf8(ref Utf8BufferWriter w, TimeSpan length, byte[] lineEnding, bool needsLeadingNewline)
    {
        long mm = (long)length.TotalMinutes;
        int ss = length.Seconds;
        if (needsLeadingNewline) w.Append(lineEnding);
        w.Append("[length:"u8);
        w.AppendInvariant(mm, "D2");
        w.Append((byte)':');
        w.AppendInvariant(ss, "D2");
        w.Append((byte)']');
    }

    private static void AppendOffsetUtf8(ref Utf8BufferWriter w, TimeSpan offset, byte[] lineEnding, bool needsLeadingNewline)
    {
        long ms = (long)offset.TotalMilliseconds;
        if (needsLeadingNewline) w.Append(lineEnding);
        w.Append("[offset:"u8);
        if (ms >= 0) w.Append((byte)'+');
        w.AppendInvariant(ms);
        w.Append((byte)']');
    }

    private static void RenderLyricsUtf8(LrcDocument document, ref Utf8BufferWriter w,
        LrcWriteOptions options, byte[] lineEnding)
    {
        var lastVoice = LrcVoice.Default;
        ReadOnlySpan<char> tsFormat = options.TimestampPrecision == LrcTimestampPrecision.Milliseconds
            ? "F" : "G";

        // Span enumeration on the no-collapse path avoids the boxed IEnumerator<LrcLine>
        // that would otherwise be allocated by the IEnumerable cast.
        if (!options.CollapseIdenticalLines)
        {
            bool first = true;
            foreach (var line in document.Lines.AsSpan())
            {
                if (!first) w.Append(lineEnding);
                first = false;
                RenderLineUtf8(line, ref w, options, tsFormat, ref lastVoice);
            }
        }
        else
        {
            bool first = true;
            foreach (var line in CollapseIdentical(document.Lines))
            {
                if (!first) w.Append(lineEnding);
                first = false;
                RenderLineUtf8(line, ref w, options, tsFormat, ref lastVoice);
            }
        }
    }

    private static void RenderLineUtf8(LrcLine line, ref Utf8BufferWriter w,
        LrcWriteOptions options, ReadOnlySpan<char> tsFormat, ref LrcVoice lastVoice)
    {
        // Timestamps
        foreach (var ts in line.Timestamps)
        {
            w.Append((byte)'[');
            w.AppendInvariant(ts, tsFormat);
            w.Append((byte)']');
        }

        // Voice marker
        if (options.EmitVoiceMarkers && line.EffectiveVoice != LrcVoice.Default)
        {
            if (!options.VoiceMarkerOnChangeOnly || line.EffectiveVoice != lastVoice)
            {
                byte marker = line.EffectiveVoice switch
                {
                    LrcVoice.Male => (byte)'M',
                    LrcVoice.Female => (byte)'F',
                    LrcVoice.Duet => (byte)'D',
                    _ => 0,
                };
                if (marker != 0)
                {
                    w.Append(marker); w.Append((byte)':'); w.Append((byte)' ');
                    lastVoice = line.EffectiveVoice;
                }
            }
        }

        // Content
        switch (line)
        {
            case LrcPlainLine plain:
                w.AppendText(plain.Text);
                break;
            case LrcEnhancedLine enhanced:
                foreach (var word in enhanced.Words)
                {
                    w.Append((byte)'<');
                    w.AppendInvariant(word.Timestamp, tsFormat);
                    w.Append((byte)'>');
                    w.AppendText(word.Text);
                }
                break;
        }
    }

    // -------------------------------------------------------------------------
    // Shared helpers
    // -------------------------------------------------------------------------

    private static readonly byte[] LfBytes = "\n"u8.ToArray();
    private static readonly byte[] CrlfBytes = "\r\n"u8.ToArray();
    private static readonly byte[] CrBytes = "\r"u8.ToArray();
    private static readonly byte[] SystemBytes = Environment.NewLine switch
    {
        "\r\n" => "\r\n"u8.ToArray(),
        "\r" => "\r"u8.ToArray(),
        _ => "\n"u8.ToArray(),
    };

    internal static string ResolveLineEnding(LrcLineEnding ending) => ending switch
    {
        LrcLineEnding.Lf => "\n",
        LrcLineEnding.Crlf => "\r\n",
        LrcLineEnding.Cr => "\r",
        LrcLineEnding.System =>
            Environment.NewLine is "\n" or "\r\n" or "\r" ? Environment.NewLine : "\n",
        _ => "\n",
    };

    private static byte[] ResolveLineEndingBytes(LrcLineEnding ending) => ending switch
    {
        LrcLineEnding.Lf => LfBytes,
        LrcLineEnding.Crlf => CrlfBytes,
        LrcLineEnding.Cr => CrBytes,
        LrcLineEnding.System => SystemBytes,
        _ => LfBytes,
    };

    internal static bool IsStronglyTypedKey(string key) => key switch
    {
        "ti" or "ar" or "al" or "au" or "lr" or "by" or "re" or "tool" or "ve" or "length" or "offset" => true,
        _ => false,
    };

    /// <summary>Rents a buffer from <see cref="ArrayPool{T}"/>, copies all non-strongly-typed
    /// raw tags, sorts them in place by key (Ordinal). Caller must Return when count > 0.</summary>
    internal static int RentSortedNonTypedTags(LrcMetadata metadata, out LrcTag[] rented)
    {
        int count = 0;
        foreach (var tag in metadata.RawTags)
            if (!IsStronglyTypedKey(tag.Key)) count++;
        if (count == 0) { rented = Array.Empty<LrcTag>(); return 0; }

        rented = ArrayPool<LrcTag>.Shared.Rent(count);
        int i = 0;
        foreach (var tag in metadata.RawTags)
            if (!IsStronglyTypedKey(tag.Key)) rented[i++] = tag;

        // Insertion sort over [0, count) — typical metadata has < 5 raw tags.
        var span = rented.AsSpan(0, count);
        for (int j = 1; j < span.Length; j++)
        {
            var k = span[j];
            int p = j - 1;
            while (p >= 0 && string.CompareOrdinal(span[p].Key, k.Key) > 0)
            {
                span[p + 1] = span[p];
                p--;
            }
            span[p + 1] = k;
        }
        return count;
    }

    /// <summary>Stream-fold consecutive identical lines (same type, voice, and content) into
    /// a single multi-timestamp line. Driven by <see cref="LrcWriteOptions.CollapseIdenticalLines"/>.</summary>
    internal static IEnumerable<LrcLine> CollapseIdentical(IEnumerable<LrcLine> source)
    {
        LrcLine? buffered = null;
        foreach (var line in source)
        {
            if (buffered is null)
            {
                buffered = line;
                continue;
            }
            if (CanCollapse(buffered, line))
            {
                buffered = MergeTimestamps(buffered, line);
            }
            else
            {
                yield return buffered;
                buffered = line;
            }
        }
        if (buffered is not null) yield return buffered;
    }

    private static bool CanCollapse(LrcLine a, LrcLine b)
    {
        if (a.GetType() != b.GetType()) return false;
        if (a.EffectiveVoice != b.EffectiveVoice) return false;
        return (a, b) switch
        {
            (LrcPlainLine pa, LrcPlainLine pb) =>
                string.Equals(pa.Text, pb.Text, StringComparison.Ordinal),
            (LrcEnhancedLine ea, LrcEnhancedLine eb) => ea.Words.Equals(eb.Words),
            _ => false,
        };
    }

    private static LrcLine MergeTimestamps(LrcLine a, LrcLine b)
    {
        var merged = a.Timestamps.AsImmutableArray().AddRange(b.Timestamps.AsImmutableArray());
        return a switch
        {
            LrcPlainLine p => p with { Timestamps = merged },
            LrcEnhancedLine e => e with { Timestamps = merged },
            _ => a,
        };
    }
}
