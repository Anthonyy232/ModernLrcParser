using System.Collections.Immutable;
using System.Diagnostics.Contracts;
using ModernLrc.Diagnostics;
using ModernLrc.Model;

namespace ModernLrc;

public static partial class LrcWriter
{
    /// <summary>O(lines + words + tags) char-length estimate. Pure.</summary>
    [Pure]
    public static int EstimateSize(LrcDocument document, LrcWriteOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(document);
        options ??= LrcWriteOptions.Default;

        int total = 0;
        // Metadata (rough — overhead + value length per tag)
        var meta = document.Metadata;
        if (meta.Title is not null) total += 6 + meta.Title.Length + 1;
        if (meta.Artist is not null) total += 6 + meta.Artist.Length + 1;
        if (meta.Album is not null) total += 6 + meta.Album.Length + 1;
        if (meta.Author is not null) total += 6 + meta.Author.Length + 1;
        if (meta.Lyricist is not null) total += 6 + meta.Lyricist.Length + 1;
        if (meta.Length is not null) total += 14;
        if (meta.CreatedBy is not null) total += 6 + meta.CreatedBy.Length + 1;
        if (meta.Offset != TimeSpan.Zero) total += 14;
        if (meta.Tool is not null) total += 6 + meta.Tool.Length + 1;
        if (meta.Version is not null) total += 6 + meta.Version.Length + 1;
        foreach (var tag in meta.RawTags)
            total += 4 + tag.Key.Length + tag.Value.Length + 1;

        if (total > 0 && document.Lines.Count > 0) total += 1; // separator blank line

        // Lyrics — 10 chars per [mm:ss.xx] (or 11 for ms) + voice marker (3) + text
        int tsLen = options.TimestampPrecision == LrcTimestampPrecision.Milliseconds ? 11 : 10;
        foreach (var line in document.Lines)
        {
            total += tsLen;
            if (options.EmitVoiceMarkers && line.EffectiveVoice != LrcVoice.Default)
                total += 3;
            switch (line)
            {
                case LrcPlainLine plain:
                    total += plain.Text.Length;
                    break;
                case LrcEnhancedLine enhanced:
                    foreach (var word in enhanced.Words)
                        total += tsLen + 2 + word.Text.Length;
                    break;
            }
            total += 2; // line ending (max CRLF)
        }
        return total;
    }

    /// <summary>O(lines + words + tags) byte-length estimate (UTF-8 worst-case: up to 4 bytes per char + headroom).</summary>
    [Pure]
    public static int EstimateByteSize(LrcDocument document, LrcWriteOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(document);
        // UTF-8: every char is 1-4 bytes; estimate at 4× chars + 16-byte headroom.
        return (int)Math.Min((long)EstimateSize(document, options) * 4 + 16, int.MaxValue);
    }

    /// <summary>Pre-flight diagnostics (LRC0090 unrepresentable voice transitions). Pure — no rendering.</summary>
    [Pure]
    public static ImmutableArray<LrcDiagnostic> ValidateForWrite(LrcDocument document, LrcWriteOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(document);
        options ??= LrcWriteOptions.Default;

        if (!options.EmitVoiceMarkers) return ImmutableArray<LrcDiagnostic>.Empty;

        var builder = ImmutableArray.CreateBuilder<LrcDiagnostic>();
        var lastEmitted = LrcVoice.Default;
        for (int i = 0; i < document.Lines.Count; i++)
        {
            var line = document.Lines[i];
            if (line.EffectiveVoice == LrcVoice.Default && lastEmitted != LrcVoice.Default)
            {
                // Walaoke has no "clear voice" marker, so this transition is unrepresentable
                // (LRC0090). lastEmitted is intentionally left unchanged — a re-parse of the
                // emitted text would continue to see the previously emitted voice.
                builder.Add(new LrcDiagnostic
                {
                    Severity = LrcDiagnosticSeverity.Warning,
                    Code = LrcDiagnosticIds.UnrepresentableVoiceTransition,
                    Line = i + 1,
                    Column = 1,
                    Length = 0,
                    Message = "Voice transition to Default cannot be emitted — Walaoke has no clear-voice marker.",
                });
            }
            else if (line.EffectiveVoice != LrcVoice.Default && line.EffectiveVoice != lastEmitted)
            {
                lastEmitted = line.EffectiveVoice;
            }
        }
        return builder.ToImmutable();
    }

}
