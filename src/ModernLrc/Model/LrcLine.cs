namespace ModernLrc.Model;

/// <summary>A single lyric line. Sealed hierarchy: every <see cref="LrcLine"/> is exactly
/// one of <see cref="LrcPlainLine"/> or <see cref="LrcEnhancedLine"/>.</summary>
/// <remarks>The <see cref="Timestamps"/> collection always carries ≥ 1 element when the
/// line was produced by the parser or <see cref="LrcDocumentBuilder"/>; the type itself does
/// not validate this on direct construction. Code that consumes user-supplied
/// <see cref="LrcLine"/> instances should treat <c>Timestamps[0]</c> as a precondition.</remarks>
public abstract record LrcLine
{
    /// <summary>Timestamps that select this line (≥ 1 element). The parser and builder maintain
    /// this invariant; the type itself does not validate the count on assignment.</summary>
    public required EquatableArray<LrcTimestamp> Timestamps { get; init; }

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
