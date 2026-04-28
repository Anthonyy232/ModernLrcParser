namespace ModernLrc.Model;

/// <summary>A single lyric line. Sealed hierarchy: every <see cref="LrcLine"/> is exactly
/// one of <see cref="LrcPlainLine"/> or <see cref="LrcEnhancedLine"/>.</summary>
/// <remarks>Each line carries exactly one <see cref="Timestamp"/>. LRC files written as
/// <c>[t1][t2]text</c> are parsed as N lines sharing the same content; the writer can
/// re-collapse them via <see cref="LrcWriteOptions.CollapseIdenticalLines"/>.</remarks>
public abstract record LrcLine
{
    /// <summary>The timestamp at which this line plays.</summary>
    public required LrcTimestamp Timestamp { get; init; }

    /// <summary>Resolved voice state — either propagated from a prior explicit marker or the document default.</summary>
    public LrcVoice EffectiveVoice { get; init; } = LrcVoice.Default;

    private protected LrcLine() { }
}

/// <summary>A simple LRC line with a single text body verbatim from after the last <c>]</c>
/// to the line terminator (no trim).</summary>
public sealed record LrcPlainLine : LrcLine
{
    /// <summary>Verbatim text, including any leading/trailing whitespace within the line.</summary>
    public required string Text { get; init; }
}

/// <summary>An Enhanced LRC line carrying word-level timing. Each word's <see cref="LrcWord.Text"/>
/// includes trailing whitespace up to the next <c>&lt;</c> marker so concatenation reproduces
/// the source line exactly.</summary>
public sealed record LrcEnhancedLine : LrcLine
{
    /// <summary>Words in source order.</summary>
    public required EquatableArray<LrcWord> Words { get; init; }
}
