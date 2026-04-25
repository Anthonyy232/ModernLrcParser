using ModernLrc.Model;

namespace ModernLrc;

/// <summary>Root model: metadata block + sorted-by-first-timestamp lines.
/// The parser and <see cref="LrcDocumentBuilder"/> maintain the sort guarantee;
/// the type itself does not validate ordering on assignment.</summary>
/// <remarks><para>Documents are immutable records — use <c>with</c> expressions or
/// <see cref="LrcDocumentBuilder"/> to derive new instances.</para>
/// <para>Use the helpers in <see cref="ModernLrc.Model.LrcDocumentExtensions"/> for playback
/// queries (<c>FindLineAt</c>, <c>LinesInRange</c>, <c>GetEffectiveTime</c>).</para></remarks>
public sealed record LrcDocument
{
    /// <summary>Singleton empty document.</summary>
    public static LrcDocument Empty { get; } = new();

    /// <summary>Document metadata (default: <see cref="LrcMetadata.Empty"/>).</summary>
    public LrcMetadata Metadata { get; init; } = LrcMetadata.Empty;

    /// <summary>Lyric lines, sorted ascending by first timestamp. Sort guarantee is supplied
    /// by the parser and by <see cref="LrcDocumentBuilder.Build"/>; constructing a document
    /// directly bypasses that check.</summary>
    public EquatableArray<LrcLine> Lines { get; init; } = EquatableArray<LrcLine>.Empty;
}
